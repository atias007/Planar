using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.General;
using Planar.Service.Listeners.Base;
using Planar.Service.Monitor;
using Quartz;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Listeners
{
    internal class SchedulerListener : BaseListener<SchedulerListener>, ISchedulerListener
    {
        private const string _cacheKey = "{0}_{1}";

        public SchedulerListener(IServiceScopeFactory serviceScopeFactory, ILogger<SchedulerListener> logger) : base(serviceScopeFactory, logger)
        {
        }

        public string Name => nameof(SchedulerListener);

        public Task JobAdded(IJobDetail jobDetail, CancellationToken cancellationToken = default)
        {
            if (IsSystemJob(jobDetail)) { return Task.CompletedTask; }
            var id = JobKeyHelper.GetJobId(jobDetail);
            if (IsLock(nameof(JobAdded), id)) { return Task.CompletedTask; }

            var info = new MonitorSystemInfo
            (
                "Job {{JobGroup}}.{{JobName}} (Id: {{JobId}}) with description {{Description}} was added"
            );

            info.MessagesParameters.Add("JobGroup", jobDetail.Key.Group);
            info.MessagesParameters.Add("JobName", jobDetail.Key.Name);
            info.MessagesParameters.Add("JobId", id);
            info.MessagesParameters.Add("Description", jobDetail.Description);
            info.AddMachineName();

            return SafeSystemScan(MonitorEvents.JobAdded, info, null);
        }

        public Task JobDeleted(JobKey jobKey, CancellationToken cancellationToken = default)
        {
            if (IsLock(nameof(JobDeleted), jobKey.ToString())) { return Task.CompletedTask; }
            var info = GetJobKeyMonitorSystemInfo(jobKey, "deleted");
            return SafeSystemScan(MonitorEvents.JobDeleted, info, null);
        }

        public Task JobInterrupted(JobKey jobKey, CancellationToken cancellationToken = default)
        {
            if (IsLock(nameof(JobInterrupted), jobKey.ToString())) { return Task.CompletedTask; }
            var info = GetJobKeyMonitorSystemInfo(jobKey, "interrupted");
            return SafeSystemScan(MonitorEvents.JobCanceled, info, null);
        }

        public Task JobPaused(JobKey jobKey, CancellationToken cancellationToken = default)
        {
            if (IsLock(nameof(JobPaused), jobKey.ToString())) { return Task.CompletedTask; }
            var info = GetJobKeyMonitorSystemInfo(jobKey, "paused");
            return SafeSystemScan(MonitorEvents.JobPaused, info, null);
        }

        public Task JobResumed(JobKey jobKey, CancellationToken cancellationToken = default)
        {
            if (IsLock(nameof(JobResumed), jobKey.ToString())) { return Task.CompletedTask; }
            var info = GetJobKeyMonitorSystemInfo(jobKey, "resumed");
            return SafeSystemScan(MonitorEvents.JobPaused, info, null);
        }

        public Task JobScheduled(ITrigger trigger, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task JobsPaused(string jobGroup, CancellationToken cancellationToken = default)
        {
            if (IsLock(nameof(JobsPaused), jobGroup)) { return Task.CompletedTask; }
            var info = GetSimpleMonitorSystemInfo("Job group {{JobGroup}} was paused");
            return SafeSystemScan(MonitorEvents.JobGroupPaused, info, null);
        }

        public Task JobsResumed(string jobGroup, CancellationToken cancellationToken = default)
        {
            if (IsLock(nameof(JobsResumed), jobGroup)) { return Task.CompletedTask; }
            var info = GetSimpleMonitorSystemInfo("Job group {{JobGroup}} was resumed");
            return SafeSystemScan(MonitorEvents.JobGroupResumed, info, null);
        }

        public Task JobUnscheduled(TriggerKey triggerKey, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SchedulerError(string msg, SchedulerException cause, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SchedulerInStandbyMode(CancellationToken cancellationToken = default)
        {
            if (IsLock(nameof(SchedulerInStandbyMode), null)) { return Task.CompletedTask; }
            var info = GetSimpleMonitorSystemInfo("Scheduler is in standby mode at {{MachineName}}");
            return SafeSystemScan(MonitorEvents.SchedulerInStandbyMode, info, null);
        }

        public Task SchedulerShutdown(CancellationToken cancellationToken = default)
        {
            if (IsLock(nameof(SchedulerShutdown), null)) { return Task.CompletedTask; }
            var info = GetSimpleMonitorSystemInfo("Scheduler was shutdown at {{MachineName}}");
            return SafeSystemScan(MonitorEvents.SchedulerInStandbyMode, info, null);
        }

        public Task SchedulerShuttingdown(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SchedulerStarted(CancellationToken cancellationToken = default)
        {
            if (IsLock(nameof(SchedulerStarted), null)) { return Task.CompletedTask; }
            var info = GetSimpleMonitorSystemInfo("Scheduler was started at {{MachineName}}");
            return SafeSystemScan(MonitorEvents.SchedulerStarted, info, null);
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
            if (IsLock(nameof(TriggerPaused), triggerKey.ToString())) { return Task.CompletedTask; }
            var info = GetTriggerKeyMonitorSystemInfo(triggerKey, "paused");
            return SafeSystemScan(MonitorEvents.TriggerPaused, info, null);
        }

        public Task TriggerResumed(TriggerKey triggerKey, CancellationToken cancellationToken = default)
        {
            if (IsLock(nameof(TriggerResumed), triggerKey.ToString())) { return Task.CompletedTask; }
            var info = GetTriggerKeyMonitorSystemInfo(triggerKey, "resumed");
            return SafeSystemScan(MonitorEvents.TriggerResumed, info, null);
        }

        public Task TriggersPaused(string? triggerGroup, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task TriggersResumed(string? triggerGroup, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        private static MonitorSystemInfo GetSimpleMonitorSystemInfo(string messageTemplate)
        {
            var info = new MonitorSystemInfo(messageTemplate);
            info.AddMachineName();
            return info;
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

        private static string GetCacheKey(string operation, string key)
        {
            return string.Format(_cacheKey, operation, key);
        }

        private static bool IsLock(string operation, string? key)
        {
            var cacheKey = GetCacheKey(operation, key ?? string.Empty);
            return LockUtil.TryLock(cacheKey, TimeSpan.FromSeconds(10));
        }
    }
}