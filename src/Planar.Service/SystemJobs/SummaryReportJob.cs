using MailKit.Net.Smtp;
using Microsoft.Extensions.DependencyInjection;
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
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs
{
    public sealed class SummaryReportJob : SystemJob, IJob
    {
        private readonly ILogger<SummaryReportJob> _logger;
        private readonly IServiceScopeFactory _serviceScope;

        public SummaryReportJob(IServiceScopeFactory serviceScope, ILogger<SummaryReportJob> logger)
        {
            _logger = logger;
            _serviceScope = serviceScope;
        }

        public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
        {
            const string description = "System job for generating and send summary report";

            var jobKey = CreateJobKey<SummaryReportJob>();
            var job = await scheduler.GetJobDetail(jobKey, stoppingToken);

            if (job != null && await IsAllTriggersExists(scheduler, jobKey))
            {
                // Job & Triggers already exists
                return;
            }

            job ??= CreateJob<SummaryReportJob>(jobKey, description);

            var dailyTrigger = await GetTrigger(scheduler, ReportPeriods.Daily);
            var weeklyTrigger = await GetTrigger(scheduler, ReportPeriods.Weekly);
            var monthlyTrigger = await GetTrigger(scheduler, ReportPeriods.Monthly);
            var quarterlyTrigger = await GetTrigger(scheduler, ReportPeriods.Quarterly);
            var yearlyTrigger = await GetTrigger(scheduler, ReportPeriods.Yearly);

            var triggers = new[] { dailyTrigger, weeklyTrigger, monthlyTrigger, quarterlyTrigger, yearlyTrigger };

            MonitorUtil.Lock(job.Key, 5, MonitorEvents.JobAdded);
            await scheduler.ScheduleJob(job, triggers, replace: true, stoppingToken);
            await PauseDisabledTrigger(scheduler, dailyTrigger, stoppingToken);
            await PauseDisabledTrigger(scheduler, weeklyTrigger, stoppingToken);
            await PauseDisabledTrigger(scheduler, monthlyTrigger, stoppingToken);
            await PauseDisabledTrigger(scheduler, quarterlyTrigger, stoppingToken);
            await PauseDisabledTrigger(scheduler, yearlyTrigger, stoppingToken);
        }

        private static async Task PauseDisabledTrigger(IScheduler scheduler, ITrigger trigger, CancellationToken stoppingToken = default)
        {
            if (!IsTriggerEnabledInData(trigger))
            {
                var state = await scheduler.GetTriggerState(trigger.Key, stoppingToken);
                if (state == TriggerState.Paused) { return; }

                MonitorUtil.Lock(trigger.Key, 5, MonitorEvents.TriggerPaused);
                await scheduler.PauseTrigger(trigger.Key, stoppingToken);
            }
        }

        internal static bool IsTriggerEnabledInData(ITrigger? trigger)
        {
            if (trigger == null) { return false; }
            var exists = trigger.JobDataMap.ContainsKey(ReportConsts.EnableTriggerDataKey);
            if (!exists) { return false; }
            var result = trigger.JobDataMap.GetBoolean(ReportConsts.EnableTriggerDataKey);
            return result;
        }

        private static string GetTriggerGroup(ITrigger? trigger)
        {
            if (trigger == null) { return string.Empty; }
            var exists = trigger.JobDataMap.ContainsKey(ReportConsts.GroupTriggerDataKey);
            if (!exists) { return string.Empty; }
            var result = trigger.JobDataMap.GetString(ReportConsts.GroupTriggerDataKey) ?? string.Empty;
            return result;
        }

        private static async Task<bool> IsAllTriggersExists(IScheduler scheduler, JobKey jobKey)
        {
            var triggers = await scheduler.GetTriggersOfJob(jobKey);
            if (triggers.Count != 5) { return false; }

            var group = triggers.Select(t => t.Key.Group).Distinct();
            if (group.Count() != 1) { return false; }
            if (group.First() != jobKey.Group) { return false; }

            if (!triggers.Any(x => x.Key.Name == ReportPeriods.Daily.ToString())) { return false; }
            if (!triggers.Any(x => x.Key.Name == ReportPeriods.Weekly.ToString())) { return false; }
            if (!triggers.Any(x => x.Key.Name == ReportPeriods.Monthly.ToString())) { return false; }
            if (!triggers.Any(x => x.Key.Name == ReportPeriods.Quarterly.ToString())) { return false; }
            if (!triggers.Any(x => x.Key.Name == ReportPeriods.Yearly.ToString())) { return false; }

            return true;
        }

        private static async Task<ITrigger> GetTrigger(IScheduler scheduler, ReportPeriods period)
        {
            var triggerKey = new TriggerKey(period.ToString(), ReportNames.Summary.ToString());
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

        private static string GetCronExpression(ReportPeriods period)
        {
            const string dailyExpression = "3 0 0 ? * * *";
            const string weeklyExpression = "3 0 0 ? * SUN *";
            const string monthlyExpressin = "3 0 0 1 * ? *";
            const string quarterlyExpressinn = "3 0 0 1 1,4,7,10 ? *";
            const string yearlyExpression = "3 0 0 1 1 ? *";

            return period switch
            {
                ReportPeriods.Weekly => weeklyExpression,
                ReportPeriods.Monthly => monthlyExpressin,
                ReportPeriods.Quarterly => quarterlyExpressinn,
                ReportPeriods.Yearly => yearlyExpression,
                _ => dailyExpression,
            };
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var dateScope = GetDateScope(context);
                var emailsTask = GetEmails(context);
                var report = new SummaryReport(_serviceScope);
                var main = await report.Generate(dateScope);
                await SendReport(main, await emailsTask);
                _logger?.LogInformation("Summary report send via smtp");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fail to send summary report: {Message}", ex.Message);
            }
        }

        private static DateScope GetDateScope(IJobExecutionContext context)
        {
            var fromExists = context.MergedJobDataMap.ContainsKey(ReportConsts.FromDateDataKey);
            var toExists = context.MergedJobDataMap.ContainsKey(ReportConsts.ToDateDataKey);
            var periodExists = context.MergedJobDataMap.ContainsKey(ReportConsts.PeriodDataKey);

            // no period & no from/to
            if (!periodExists && (!fromExists || !toExists))
            {
                throw new InvalidOperationException($"job data key '{ReportConsts.FromDateDataKey}' or '{ReportConsts.ToDateDataKey}' (report from/to date) could not found while no period data key '{ReportConsts.PeriodDataKey}' found");
            }

            var formString = context.MergedJobDataMap.GetString(ReportConsts.FromDateDataKey) ?? string.Empty;
            var toString = context.MergedJobDataMap.GetString(ReportConsts.ToDateDataKey) ?? string.Empty;

            var from = fromExists ? DateTime.Parse(formString, CultureInfo.CurrentCulture) : (DateTime?)null;
            var to = toExists ? DateTime.Parse(toString, CultureInfo.CurrentCulture) : (DateTime?)null;

            if (periodExists)
            {
                var periodString = context.MergedJobDataMap.GetString(ReportConsts.PeriodDataKey);
                if (!Enum.TryParse<ReportPeriods>(periodString, ignoreCase: true, out var period))
                {
                    throw new InvalidOperationException($"job data key '{ReportConsts.PeriodDataKey}' (report period) value '{periodString}' is not valid");
                }

                var scopes = GetDateScope(period, to);
                return scopes;
            }

            return new DateScope(from!.Value, to!.Value, "Custom Date Scope");
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

        private async Task SendReport(string html, IEnumerable<string> emails)
        {
            var smtp = AppSettings.Smtp;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));

            foreach (var recipient in emails)
            {
                if (string.IsNullOrEmpty(recipient)) { continue; }
                if (!ValidationUtil.IsValidEmail(recipient))
                {
                    _logger.LogWarning("send report warning: email address '{Email}' is not valid", recipient);
                }
                else
                {
                    message.To.Add(new MailboxAddress(recipient, recipient));
                }
            }

            message.Subject = $"Planar Summary Daily Report";
            var body = new BodyBuilder
            {
                HtmlBody = html,
            }.ToMessageBody();

            message.Body = body;

            using var client = new SmtpClient();
            var tokenSource = new CancellationTokenSource(30000);

            client.Connect(smtp.Host, port: smtp.Port, useSsl: smtp.UseSsl, tokenSource.Token);
            client.Authenticate(smtp.Username, smtp.Password, tokenSource.Token);
            await client.SendAsync(message, tokenSource.Token);
            client.Disconnect(quit: true, tokenSource.Token);
        }

        private async Task<IEnumerable<string>> GetEmails(IJobExecutionContext context)
        {
            var groupName = GetTriggerGroup(context.Trigger);
            if (string.IsNullOrEmpty(groupName))
            {
                throw new InvalidOperationException($"job data key '{ReportConsts.GroupTriggerDataKey}' (name of distribution group) could not found");
            }

            using var scope = _serviceScope.CreateScope();
            var groupData = scope.ServiceProvider.GetRequiredService<GroupData>();
            if (string.IsNullOrEmpty(groupName))
            {
                throw new InvalidOperationException($"distribution group '{groupName}' could not found");
            }

            var id = await groupData.GetGroupId(groupName);
            var group = await groupData.GetGroupWithUsers(id)
                ?? throw new InvalidOperationException($"distribution group '{groupName}' could not found");

            if (!group.Users.Any())
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
    }
}