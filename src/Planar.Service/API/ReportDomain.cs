using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.Monitor;
using Planar.Service.Reports;
using Planar.Service.SystemJobs;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.API;

public class ReportDomain(IServiceProvider serviceProvider) : BaseBL<ReportDomain, ReportData>(serviceProvider)
{
    private const string group = "group";

    private static ReportNames ValidateReportName(string name)
    {
        if (!Enum.TryParse<ReportNames>(name, ignoreCase: true, out var reportName))
        {
            throw new RestNotFoundException($"report name '{name}' is not valid");
        }

        return reportName;
    }

    public async Task Update(string name, UpdateReportRequest request)
    {
        // validate report name
        var reportName = ValidateReportName(name);

        // validate group & emails
        if (!string.IsNullOrWhiteSpace(request.Group))
        {
            await ValidateGroupAndEmails(request.Group);
        }

        // get trigger
        var requestPeriod = Enum.Parse<ReportPeriods>(request.Period, true);
        var triggerKey = new TriggerKey(requestPeriod.ToString(), reportName.ToString());
        var scheduler = Resolve<IScheduler>();
        var trigger = await scheduler.GetTrigger(triggerKey);
        var triggerId = TriggerHelper.GetTriggerId(trigger);

        if (trigger == null || string.IsNullOrEmpty(triggerId))
        {
            throw new InvalidOperationException($"trigger with id '{triggerId}' is not exists");
        }

        // get current enable value if its null
        request.Enable ??= trigger.JobDataMap.GetBoolean(ReportConsts.EnableTriggerDataKey);
        if (!request.Enable.GetValueOrDefault() && request.HourOfDay.HasValue)
        {
            throw new RestValidationException("hourOfDay", $"report hour is not valid for disabled report");
        }

        // validate mandatory group
        var existsGroup = trigger.JobDataMap.GetString(ReportConsts.GroupTriggerDataKey);
        if (request.Enable.Value && string.IsNullOrEmpty(existsGroup) && string.IsNullOrWhiteSpace(request.Group))
        {
            throw new RestValidationException(group, $"group is mandatory to enable report");
        }

        request.Group ??= existsGroup;

        var groupDal = Resolve<GroupData>();
        var groupName =
            string.IsNullOrWhiteSpace(request.Group) ?
            string.Empty :
            (await groupDal.GetGroup(request.Group))?.Name;

        var triggerDomain = Resolve<TriggerDomain>();
        var putDataRequest = new JobOrTriggerDataRequest
        {
            DataKey = ReportConsts.EnableTriggerDataKey,
            DataValue = request.Enable.ToString(),
            Id = triggerId
        };
        await triggerDomain.PutData(putDataRequest, JobDomain.PutMode.Update, skipSystemCheck: true);

        putDataRequest = new JobOrTriggerDataRequest
        {
            DataKey = ReportConsts.GroupTriggerDataKey,
            DataValue = groupName,
            Id = triggerId
        };
        await triggerDomain.PutData(putDataRequest, JobDomain.PutMode.Update, skipSystemCheck: true);

        if (request.HourOfDay.HasValue && request.HourOfDay.Value != trigger.StartTimeUtc.Hour)
        {
            var cronExpression = BaseReportJob.GetCronExpression(requestPeriod, request.HourOfDay.Value);
            var newTrigger = trigger.GetTriggerBuilder()
                .WithCronSchedule(cronExpression)
                .Build();

            await scheduler.RescheduleJob(trigger.Key, newTrigger);
        }

        if (request.Enable.Value)
        {
            await ResumeEnabledTrigger(scheduler, trigger);
        }
        else
        {
            await PauseDisabledTrigger(scheduler, trigger);
        }
    }

    public async Task<IEnumerable<ReportsStatus>> GetReport(string name)
    {
        var reportName = ValidateReportName(name);

        var jobKey = new JobKey($"{reportName}ReportJob", Consts.PlanarSystemGroup);
        var scheduler = Resolve<IScheduler>();
        var triggers = await scheduler.GetTriggersOfJob(jobKey);

        if (triggers == null || triggers.Count == 0)
        {
            throw new InvalidOperationException($"could not found triggers for report with key {jobKey}. report name '{name}'");
        }

        var result = triggers
            .Select(t => new ReportsStatus
            {
                Period = t.JobDataMap.GetString(ReportConsts.PeriodDataKey)?.ToLower() ?? string.Empty,
                Enabled = t.JobDataMap.GetBoolean(ReportConsts.EnableTriggerDataKey),
                Group = t.JobDataMap.GetString(ReportConsts.GroupTriggerDataKey),
                NextRunning =
                     t.JobDataMap.GetBoolean(ReportConsts.EnableTriggerDataKey) ?
                    t.GetNextFireTimeUtc()?.LocalDateTime :
                    null,
            });

        var final = result
            .Select(t => new
            {
                Entity = t,
                Order = Enum.TryParse<ReportPeriods>(t.Period, ignoreCase: true, out var period) ?
                    (int)period :
                    -1
            })
            .OrderBy(t => t.Order)
            .Select(t => t.Entity);

        return final;
    }

    public async Task Run(string name, RunReportRequest request)
    {
        var reportName = ValidateReportName(name);
        var jobKey = new JobKey($"{reportName}ReportJob", Consts.PlanarSystemGroup);

        if (string.IsNullOrWhiteSpace(request.Group))
        {
            var scheduler = Resolve<IScheduler>();
            var triggerKey = new TriggerKey(ReportPeriods.Daily.ToString(), reportName.ToString());
            var trigger = await scheduler.GetTrigger(triggerKey);
            request.Group = trigger?.JobDataMap.GetString(ReportConsts.GroupTriggerDataKey);
        }

        // validate group & emails
        if (string.IsNullOrWhiteSpace(request.Group))
        {
            throw new RestValidationException(group, $"group is mandatory to run report");
        }

        await ValidateGroupAndEmails(request.Group);

        // validate period & dates not null
        var allEmpty = string.IsNullOrEmpty(request.Period) && request.FromDate == null && request.ToDate == null;
        if (allEmpty)
        {
            throw new RestValidationException("period", $"if from & to dates has no value, period is mandatory to run report");
        }

        // validate period & dates not all has value
        var allHasValue = !string.IsNullOrEmpty(request.Period) && request.FromDate != null && request.ToDate != null;
        if (allHasValue)
        {
            throw new RestValidationException("period", $"if period has value, 'to date' must not have value");
        }

        var jobDomain = Resolve<JobDomain>();
        var invokeRequest = new InvokeJobRequest
        {
            Id = jobKey.ToString(),
            Data = new Dictionary<string, string?>
            {
                { ReportConsts.GroupTriggerDataKey, request.Group },
            }
        };

        if (!string.IsNullOrEmpty(request.Period))
        {
            var period = Enum.Parse<ReportPeriods>(request.Period, ignoreCase: true);
            invokeRequest.Data.Add(ReportConsts.PeriodDataKey, period.ToString());
        }

        if (request.FromDate.HasValue)
        {
            invokeRequest.Data.Add(ReportConsts.FromDateDataKey, request.FromDate.Value.ToShortDateString());
        }

        if (request.ToDate.HasValue)
        {
            invokeRequest.Data.Add(ReportConsts.ToDateDataKey, request.ToDate.Value.ToShortDateString());
        }

        await jobDomain.Invoke(invokeRequest);
    }

    public static IEnumerable<string> GetReports()
    {
        var items = Enum.GetNames<ReportNames>()
            .Select(n => n.ToLower())
            .OrderBy(n => n);

        return items;
    }

    public static IEnumerable<string> GetPeriods()
    {
        var items = Enum.GetNames<ReportPeriods>()
            .Select(n => n.ToLower());

        return items;
    }

    private static async Task PauseDisabledTrigger(IScheduler scheduler, ITrigger trigger)
    {
        var state = await scheduler.GetTriggerState(trigger.Key);
        if (state == TriggerState.Paused) { return; }

        MonitorUtil.Lock(trigger.Key, 5, MonitorEvents.TriggerPaused);
        await scheduler.PauseTrigger(trigger.Key);
    }

    private static async Task ResumeEnabledTrigger(IScheduler scheduler, ITrigger trigger)
    {
        var state = await scheduler.GetTriggerState(trigger.Key);
        if (state != TriggerState.Paused) { return; }

        MonitorUtil.Lock(trigger.Key, 5, MonitorEvents.TriggerResumed);
        await scheduler.ResumeTrigger(trigger.Key);
    }

    private async Task ValidateGroupAndEmails(string groupName)
    {
        // validate group exists
        var groupDal = Resolve<GroupData>();
        var id = await groupDal.GetGroupId(groupName);
        var group =
            await groupDal.GetGroupWithUsers(id)
            ?? throw new RestValidationException(group, $"group with name '{groupName}' is not exists");

        // get all emails & validate
        var emails1 = group.Users.Select(u => u.EmailAddress1);
        var emails2 = group.Users.Select(u => u.EmailAddress1);
        var emails3 = group.Users.Select(u => u.EmailAddress1);

        var allEmails = emails1.Concat(emails2).Concat(emails3)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct();

        if (!allEmails.Any())
        {
            throw new RestValidationException(group, $"group with name '{groupName}' has no users with valid emails");
        }
    }
}