﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.Data;
using Planar.Service.General;
using Planar.Service.Monitor;
using Planar.Service.Reports;
using Planar.Service.Validation;
using Quartz;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs;

public abstract class BaseReportJob(IServiceScopeFactory serviceScope, ILogger logger) : SystemJob
{
    protected readonly ILogger _logger = logger;
    protected readonly IServiceScopeFactory _serviceScope = serviceScope;

    internal static string GetCronExpression(ReportPeriods period, int hour = 7)
    {
        const string dailyExpression = "3 0 [hour] ? * * *";
        const string weeklyExpression = "3 0 [hour] ? * SUN *";
        const string monthlyExpressin = "3 0 [hour] 1 * ? *";
        const string quarterlyExpressinn = "3 0 [hour] 1 1,4,7,10 ? *";
        const string yearlyExpression = "3 0 [hour] 1 1 ? *";

        var result = period switch
        {
            ReportPeriods.Weekly => weeklyExpression,
            ReportPeriods.Monthly => monthlyExpressin,
            ReportPeriods.Quarterly => quarterlyExpressinn,
            ReportPeriods.Yearly => yearlyExpression,
            _ => dailyExpression,
        };

        result = result.Replace("[hour]", hour.ToString());
        return result;
    }

    protected static DateScope GetDateScope(IJobExecutionContext context)
    {
        var fromExists = context.MergedJobDataMap.ContainsKey(ReportConsts.FromDateDataKey);
        var toExists = context.MergedJobDataMap.ContainsKey(ReportConsts.ToDateDataKey);
        var periodExists = context.MergedJobDataMap.ContainsKey(ReportConsts.PeriodDataKey);

        // no period & no from/to
        if (!periodExists && (!fromExists || !toExists))
        {
            throw new InvalidOperationException($"job data key '{ReportConsts.FromDateDataKey}' or '{ReportConsts.ToDateDataKey}' (report from/to date) could not found while no period data key '{ReportConsts.PeriodDataKey}' found");
        }

        var formString = fromExists ? context.MergedJobDataMap.GetString(ReportConsts.FromDateDataKey) ?? string.Empty : string.Empty;
        var toString = toExists ? context.MergedJobDataMap.GetString(ReportConsts.ToDateDataKey) ?? string.Empty : string.Empty;

        var from = fromExists ? DateTime.Parse(formString, CultureInfo.CurrentCulture) : (DateTime?)null;
        var to = toExists ? DateTime.Parse(toString, CultureInfo.CurrentCulture) : (DateTime?)null;

        if (periodExists)
        {
            var periodString = context.MergedJobDataMap.GetString(ReportConsts.PeriodDataKey);
            if (!Enum.TryParse<ReportPeriods>(periodString, ignoreCase: true, out var period))
            {
                throw new InvalidOperationException($"job data key '{ReportConsts.PeriodDataKey}' (report period) value '{periodString}' is not valid");
            }

            var scopes = GetDateScope(period, from);
            return scopes;
        }

        return new DateScope(from!.Value, to!.Value, "Custom Date Scope");
    }

    protected static async Task<ITrigger> GetTrigger(IScheduler scheduler, ReportPeriods period, ReportNames reportName)
    {
        var triggerKey = new TriggerKey(period.ToString(), reportName.ToString());
        var existsTrigger = await scheduler.GetTrigger(triggerKey);
        var triggerId = TriggerHelper.GetTriggerId(existsTrigger) ?? ServiceUtil.GenerateId();
        var cronExpression = GetCronExpression(period);
        var enable = IsTriggerEnabledInData(existsTrigger);
        var group = GetTriggerGroup(existsTrigger);

        var trigger = TriggerBuilder.Create()
                .WithIdentity(triggerKey)
                .UsingJobData(Consts.TriggerId, triggerId)
                .UsingJobData(ReportConsts.EnableTriggerDataKey, enable.ToString())
                .UsingJobData(ReportConsts.GroupTriggerDataKey, group)
                .UsingJobData(ReportConsts.PeriodDataKey, period.ToString())
                .WithCronSchedule(cronExpression, builder => builder.WithMisfireHandlingInstructionFireAndProceed())
                .StartAt(DateTimeOffset.Now.Date.AddDays(1).AddSeconds(-1))
                .WithPriority(int.MinValue)
                .Build();

        return trigger;
    }

    protected static async Task<bool> IsAllTriggersExists(IScheduler scheduler, JobKey jobKey, ReportNames reportName)
    {
        var triggers = await scheduler.GetTriggersOfJob(jobKey);
        if (triggers.Count != 5) { return false; }

        var group = triggers.Select(t => t.Key.Group).Distinct();
        if (group.Count() != 1) { return false; }
        if (!string.Equals(group.First(), reportName.ToString(), StringComparison.OrdinalIgnoreCase)) { return false; }

        if (!triggers.Any(x => x.Key.Name == ReportPeriods.Daily.ToString())) { return false; }
        if (!triggers.Any(x => x.Key.Name == ReportPeriods.Weekly.ToString())) { return false; }
        if (!triggers.Any(x => x.Key.Name == ReportPeriods.Monthly.ToString())) { return false; }
        if (!triggers.Any(x => x.Key.Name == ReportPeriods.Quarterly.ToString())) { return false; }
        if (!triggers.Any(x => x.Key.Name == ReportPeriods.Yearly.ToString())) { return false; }

        return true;
    }

    protected static async Task PauseDisabledTrigger(IScheduler scheduler, ITrigger trigger, CancellationToken stoppingToken = default)
    {
        if (!IsTriggerEnabledInData(trigger))
        {
            var state = await scheduler.GetTriggerState(trigger.Key, stoppingToken);
            if (state == TriggerState.Paused) { return; }

            MonitorUtil.Lock(trigger.Key, 30, MonitorEvents.TriggerPaused);
            await scheduler.PauseTrigger(trigger.Key, stoppingToken);
        }
    }

    protected async Task<IEnumerable<string>> GetEmails(IJobExecutionContext context)
    {
        var groupName = GetTriggerGroup(context.Trigger);
        if (string.IsNullOrEmpty(groupName))
        {
            throw new InvalidOperationException($"job data key '{ReportConsts.GroupTriggerDataKey}' (name of distribution group) could not found");
        }

        using var scope = _serviceScope.CreateScope();
        var groupData = scope.ServiceProvider.GetRequiredService<IGroupData>();
        if (string.IsNullOrEmpty(groupName))
        {
            throw new InvalidOperationException($"distribution group '{groupName}' could not found");
        }

        var id = await groupData.GetGroupId(groupName);
        var group = await groupData.GetGroupWithUsers(id)
            ?? throw new InvalidOperationException($"distribution group '{groupName}' could not found");

        if (group.Users.Count == 0)
        {
            throw new InvalidOperationException($"distribution group '{groupName}' has no users");
        }

        var emails1 = group.Users.Select(x => x.EmailAddress1 ?? string.Empty);
        var emails2 = group.Users.Select(x => x.EmailAddress2 ?? string.Empty);
        var emails3 = group.Users.Select(x => x.EmailAddress3 ?? string.Empty);
        var allEmails = emails1.Union(emails2).Union(emails3)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct();

        if (!allEmails.Any())
        {
            throw new InvalidOperationException($"distribution group '{groupName}' has no users with email");
        }

        return allEmails;
    }

    protected async Task SendReport(string html, IEnumerable<string> emails, ReportNames reportName, string period)
    {
        var message = new MimeMessage();

        foreach (var recipient in emails)
        {
            if (string.IsNullOrEmpty(recipient)) { continue; }
            if (!ValidationUtil.IsValidEmail(recipient))
            {
                _logger.LogWarning("send report warning: email address '{Email}' is not valid", recipient);
            }
            else
            {
                message.Bcc.Add(new MailboxAddress(recipient, recipient));
            }
        }

        var reportTitle = reportName.GetEnumDescription();
        message.Subject = $"Planar Report | {reportTitle} | {period} | {AppSettings.General.Environment}";
        var body = new BodyBuilder
        {
            HtmlBody = html,
        }.ToMessageBody();

        message.Body = body;

        var result = await SmtpUtil.SendMessage(message);
        _logger.LogDebug("SMTP send result: {Message}", result);
    }

    private static DateScope GetDateScope(ReportPeriods periods, DateTime? referenceDate)
    {
        if (referenceDate == null)
        {
            referenceDate = DateTime.Now;
        }

        var date = referenceDate.Value.Date;
        switch (periods)
        {
            default:
            case ReportPeriods.Daily:
                return new DateScope(date.AddDays(-1), date, periods);

            case ReportPeriods.Weekly:
                var from1 = date.AddDays(-(int)date.DayOfWeek);
                return new DateScope(from1, from1.AddDays(7), periods);

            case ReportPeriods.Monthly:
                var from2 = date.AddDays(1 - date.Day);
                return new DateScope(from2, from2.AddMonths(1), periods);

            case ReportPeriods.Quarterly:
                var quarter = (date.Month - 1) / 3 + 1;
                var year = date.Year;
                var from3 = new DateTime(year, quarter, 1, 0, 0, 0, DateTimeKind.Local);
                return new DateScope(from3, from3.AddMonths(3), periods);

            case ReportPeriods.Yearly:
                var year2 = date.Year;
                var from4 = new DateTime(year2, 1, 1, 0, 0, 0, DateTimeKind.Local);
                return new DateScope(from4, from4.AddYears(1), periods);
        }
    }

    private static string GetTriggerGroup(ITrigger? trigger)
    {
        if (trigger == null) { return string.Empty; }
        var exists = trigger.JobDataMap.ContainsKey(ReportConsts.GroupTriggerDataKey);
        if (!exists) { return string.Empty; }
        var result = trigger.JobDataMap.GetString(ReportConsts.GroupTriggerDataKey) ?? string.Empty;
        return result;
    }

    private static bool IsTriggerEnabledInData(ITrigger? trigger)
    {
        if (trigger == null) { return false; }
        var exists = trigger.JobDataMap.ContainsKey(ReportConsts.EnableTriggerDataKey);
        if (!exists) { return false; }
        var result = trigger.JobDataMap.GetBoolean(ReportConsts.EnableTriggerDataKey);
        return result;
    }
}

public abstract class BaseReportJob<TJob>(IServiceScopeFactory serviceScope, ILogger logger) : BaseReportJob(serviceScope, logger)
    where TJob : IJob
{
    public static async Task Schedule(IScheduler scheduler, ReportNames reportName, CancellationToken stoppingToken = default)
    {
        var description = $"System job for generating and send {reportName} report";

        var jobKey = CreateJobKey<TJob>();
        var job = await scheduler.GetJobDetail(jobKey, stoppingToken);

        if (job != null && await IsAllTriggersExists(scheduler, jobKey, reportName))
        {
            // Job & Triggers already exists
            return;
        }

        job ??= CreateJob<TJob>(jobKey, description);

        var dailyTrigger = await GetTrigger(scheduler, ReportPeriods.Daily, reportName);
        var weeklyTrigger = await GetTrigger(scheduler, ReportPeriods.Weekly, reportName);
        var monthlyTrigger = await GetTrigger(scheduler, ReportPeriods.Monthly, reportName);
        var quarterlyTrigger = await GetTrigger(scheduler, ReportPeriods.Quarterly, reportName);
        var yearlyTrigger = await GetTrigger(scheduler, ReportPeriods.Yearly, reportName);

        var triggers = new[] { dailyTrigger, weeklyTrigger, monthlyTrigger, quarterlyTrigger, yearlyTrigger };

        MonitorUtil.Lock(job.Key, 5, MonitorEvents.JobAdded);
        await scheduler.ScheduleJob(job, triggers, replace: true, stoppingToken);
        await PauseDisabledTrigger(scheduler, dailyTrigger, stoppingToken);
        await PauseDisabledTrigger(scheduler, weeklyTrigger, stoppingToken);
        await PauseDisabledTrigger(scheduler, monthlyTrigger, stoppingToken);
        await PauseDisabledTrigger(scheduler, quarterlyTrigger, stoppingToken);
        await PauseDisabledTrigger(scheduler, yearlyTrigger, stoppingToken);
    }

    protected async Task<bool> SafeExecute<TReport>(IJobExecutionContext context, ReportNames reportName)
        where TReport : BaseReport
    {
        try
        {
            var dateScope = GetDateScope(context);
            var emailsTask = GetEmails(context);
            if (Activator.CreateInstance(typeof(TReport), _serviceScope) is not TReport report)
            {
                throw new InvalidOperationException($"could not create instance of {typeof(TReport).Name} report");
            }

            var main = await report.Generate(dateScope);
            main = HtmlUtil.MinifyHtml(main);
            await SendReport(main, await emailsTask, reportName, dateScope.Period);
            _logger.LogInformation("{Name} report send via smtp", reportName.ToString().ToLower());
            SafeSetLastRun(context, _logger);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to send {Report} report: {Message}", reportName.ToString().ToLower(), ex.Message);
            return false;
        }
    }
}