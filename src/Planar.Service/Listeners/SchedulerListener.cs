using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.General;
using Planar.Service.Listeners.Base;
using Planar.Service.Monitor;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Listeners;

internal class SchedulerListener(IServiceScopeFactory serviceScopeFactory, ILogger<SchedulerListener> logger) : BaseListener<SchedulerListener>(serviceScopeFactory, logger), ISchedulerListener
{
    private const string _cacheKey = "{0}_{1}";

    public static string Name => nameof(SchedulerListener);

    public Task JobAdded(IJobDetail jobDetail, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            if (IsSystemJob(jobDetail)) { return; }
            var id = JobKeyHelper.GetJobId(jobDetail);
            if (IsLocked(nameof(JobAdded), id)) { return; }

            var info = new MonitorSystemInfo
            (
                "Job {{JobGroup}}.{{JobName}} (Id: {{JobId}}) with description {{Description}} was added"
            );

            info.MessagesParameters.Add("JobGroup", jobDetail.Key.Group);
            info.MessagesParameters.Add("JobName", jobDetail.Key.Name);
            info.MessagesParameters.Add("JobId", id);
            info.MessagesParameters.Add("Description", jobDetail.Description);
            info.AddMachineName();

            SafeSystemScan(MonitorEvents.JobAdded, info, null);
        }, cancellationToken);
    }

    public Task JobDeleted(JobKey jobKey, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            if (IsSystemJobKey(jobKey)) { return; }
            if (IsLocked(nameof(JobDeleted), jobKey.ToString())) { return; }
            var info = GetJobKeyMonitorSystemInfo(jobKey, "deleted");
            SafeSystemScan(MonitorEvents.JobDeleted, info, null);
        }, cancellationToken);
    }

    public Task JobInterrupted(JobKey jobKey, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            if (IsSystemJobKey(jobKey)) { return; }
            if (IsLocked(nameof(JobInterrupted), jobKey.ToString())) { return; }
            var info = GetJobKeyMonitorSystemInfo(jobKey, "canceled");
            SafeSystemScan(MonitorEvents.JobCanceled, info, null);
        }, cancellationToken);
    }

    public Task JobPaused(JobKey jobKey, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            if (IsSystemJobKey(jobKey)) { return; }
            if (IsLocked(nameof(JobPaused), jobKey.ToString())) { return; }
            var info = GetJobKeyMonitorSystemInfo(jobKey, "paused");
            SafeSystemScan(MonitorEvents.JobPaused, info, null);
        }, cancellationToken);
    }

    public Task JobResumed(JobKey jobKey, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            if (IsSystemJobKey(jobKey)) { return; }
            if (IsLocked(nameof(JobResumed), jobKey.ToString())) { return; }
            var info = GetJobKeyMonitorSystemInfo(jobKey, "resumed");
            SafeSystemScan(MonitorEvents.JobResumed, info, null);
        }, cancellationToken);
    }

    public Task JobScheduled(ITrigger trigger, CancellationToken cancellationToken = default)
    {
        ////if (TriggerKeyHelper.IsSystemTriggerKey(trigger.Key)) { return Task.CompletedTask; }
        return Task.CompletedTask;
    }

    public Task JobsPaused(string jobGroup, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task JobsResumed(string jobGroup, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task JobUnscheduled(TriggerKey triggerKey, CancellationToken cancellationToken = default)
    {
        ////if (TriggerKeyHelper.IsSystemTriggerKey(triggerKey)) { return Task.CompletedTask; }
        return Task.CompletedTask;
    }

    public Task SchedulerError(string msg, SchedulerException cause, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            if (IsLocked(nameof(SchedulerError), null)) { return; }
            _logger.LogError(cause, "scheduler error with message {Message}", msg);
            var info = new MonitorSystemInfo
            (
                "Scheduler has error with message '{{ErrorMessage}}' at {{MachineName}}"
            );

            info.MessagesParameters.Add("ErrorMessage", msg);
            info.AddMachineName();
            SafeSystemScan(MonitorEvents.SchedulerError, info, cause);
        }, cancellationToken);
    }

    public Task SchedulerInStandbyMode(CancellationToken cancellationToken = default)
    {
        // *** DONT USE TASK.RUN ***
        if (IsLocked(nameof(SchedulerInStandbyMode), null)) { return Task.CompletedTask; }
        var info = GetSimpleMonitorSystemInfo("Scheduler is in standby mode at {{MachineName}}");
        SafeSystemScan(MonitorEvents.SchedulerInStandbyMode, info, null);
        return Task.CompletedTask;
    }

    public Task SchedulerShutdown(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SchedulerShuttingdown(CancellationToken cancellationToken = default)
    {
        // *** DONT USE TASK.RUN ***
        if (IsLocked(nameof(SchedulerShutdown), null)) { return Task.CompletedTask; }
        var info = GetSimpleMonitorSystemInfo("Scheduler shuting down at {{MachineName}}");
        SafeSystemScan(MonitorEvents.SchedulerShutdown, info, null);
        return Task.CompletedTask;
    }

    public Task SchedulerStarted(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            if (IsLocked(nameof(SchedulerStarted), null)) { return; }
            _logger.LogInformation("scheduler started");
            var info = GetSimpleMonitorSystemInfo("Scheduler was started at {{MachineName}}");
            SafeSystemScan(MonitorEvents.SchedulerStarted, info, null);
        }, cancellationToken);
    }

    public Task SchedulerStarting(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SchedulingDataCleared(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task TriggerFinalized(ITrigger trigger, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task TriggerPaused(TriggerKey triggerKey, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            if (TriggerKeyHelper.IsSystemTriggerKey(triggerKey)) { return; }
            if (IsLocked(nameof(TriggerPaused), triggerKey.ToString())) { return; }
            var info = GetTriggerKeyMonitorSystemInfo(triggerKey, "paused");
            SafeSystemScan(MonitorEvents.TriggerPaused, info, null);
        }, cancellationToken);
    }

    public Task TriggerResumed(TriggerKey triggerKey, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            if (TriggerKeyHelper.IsSystemTriggerKey(triggerKey)) { return; }
            if (IsLocked(nameof(TriggerResumed), triggerKey.ToString())) { return; }
            var info = GetTriggerKeyMonitorSystemInfo(triggerKey, "resumed");
            SafeSystemScan(MonitorEvents.TriggerResumed, info, null);
        }, cancellationToken);
    }

    public Task TriggersPaused(string? triggerGroup, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task TriggersResumed(string? triggerGroup, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private static string GetCacheKey(string operation, string key)
    {
        return string.Format(_cacheKey, operation, key);
    }

    private static MonitorSystemInfo GetJobKeyMonitorSystemInfo(JobKey jobKey, string title)
    {
        var info = new MonitorSystemInfo
        (
            $"Job {{{{JobGroup}}}}.{{{{JobName}}}} was {title}"
        );

        info.MessagesParameters.Add("JobGroup", jobKey.Group);
        info.MessagesParameters.Add("JobName", jobKey.Name);
        info.AddMachineName();
        return info;
    }

    private static MonitorSystemInfo GetSimpleMonitorSystemInfo(string messageTemplate)
    {
        var info = new MonitorSystemInfo(messageTemplate);
        info.AddMachineName();
        return info;
    }

    private static MonitorSystemInfo GetTriggerKeyMonitorSystemInfo(TriggerKey triggerKey, string title)
    {
        var info = new MonitorSystemInfo
        (
            $"Trigger {{{{TriggerGroup}}}}.{{{{TriggerName}}}} was {title}"
        );

        info.MessagesParameters.Add("TriggerGroup", triggerKey.Group);
        info.MessagesParameters.Add("TriggerName", triggerKey.Name);
        info.AddMachineName();
        return info;
    }

    private static bool IsLocked(string operation, string? key)
    {
        var cacheKey = GetCacheKey(operation, key ?? string.Empty);
        return !LockUtil.TryLock(cacheKey, TimeSpan.FromSeconds(5));
    }
}