using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.API.Helpers;
using Planar.Service.Listeners.Base;
using Planar.Service.Monitor;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Listeners
{
    internal class SchedulerListener : BaseListener<SchedulerListener>, ISchedulerListener
    {
        public SchedulerListener(IServiceScopeFactory serviceScopeFactory, ILogger<SchedulerListener> logger) : base(serviceScopeFactory, logger)
        {
        }

        public string Name => nameof(SchedulerListener);

        public async Task JobAdded(IJobDetail jobDetail, CancellationToken cancellationToken = default)
        {
            var info = new MonitorSystemInfo
            {
                MessageTemplate = "Job {{JobGroup}}.{{JobName}} (Id: {{JobId}}) with description {{Description}} was added"
            };

            var id = JobKeyHelper.GetJobId(jobDetail);
            info.MessagesParameters.Add("JobGroup", jobDetail.Key.Group);
            info.MessagesParameters.Add("JobName", jobDetail.Key.Name);
            info.MessagesParameters.Add("JobId", id);
            info.MessagesParameters.Add("Description", jobDetail.Description);

            await SafeSystemScan(MonitorEvents.JobAdded, info, null, cancellationToken);
        }

        public async Task JobDeleted(JobKey jobKey, CancellationToken cancellationToken = default)
        {
            var info = GetJobKeyMonitorSystemInfo(jobKey, "deleted");
            await SafeSystemScan(MonitorEvents.JobDeleted, info, null, cancellationToken);
        }

        public async Task JobInterrupted(JobKey jobKey, CancellationToken cancellationToken = default)
        {
            var info = GetJobKeyMonitorSystemInfo(jobKey, "interrupted");
            await SafeSystemScan(MonitorEvents.JobInterrupted, info, null, cancellationToken);
        }

        public async Task JobPaused(JobKey jobKey, CancellationToken cancellationToken = default)
        {
            var info = GetJobKeyMonitorSystemInfo(jobKey, "paused");
            await SafeSystemScan(MonitorEvents.JobPaused, info, null, cancellationToken);
        }

        public async Task JobResumed(JobKey jobKey, CancellationToken cancellationToken = default)
        {
            var info = GetJobKeyMonitorSystemInfo(jobKey, "resumed");
            await SafeSystemScan(MonitorEvents.JobPaused, info, null, cancellationToken);
        }

        public Task JobScheduled(ITrigger trigger, CancellationToken cancellationToken = default)
        {
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
            return Task.CompletedTask;
        }

        public Task SchedulerError(string msg, SchedulerException cause, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public async Task SchedulerInStandbyMode(CancellationToken cancellationToken = default)
        {
            var info = new MonitorSystemInfo
            {
                MessageTemplate = "Scheduler is in standby mode at {{MachineName}}"
            };

            info.MessagesParameters.Add("MachineName", Environment.MachineName);

            await SafeSystemScan(MonitorEvents.SchedulerInStandbyMode, info, null, cancellationToken);
        }

        public async Task SchedulerShutdown(CancellationToken cancellationToken = default)
        {
            var info = new MonitorSystemInfo
            {
                MessageTemplate = "Scheduler was shutdown at {{MachineName}}"
            };

            info.MessagesParameters.Add("MachineName", Environment.MachineName);

            await SafeSystemScan(MonitorEvents.SchedulerInStandbyMode, info, null, cancellationToken);
        }

        public Task SchedulerShuttingdown(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public async Task SchedulerStarted(CancellationToken cancellationToken = default)
        {
            var info = new MonitorSystemInfo
            {
                MessageTemplate = "Scheduler was started at {{MachineName}}"
            };

            info.MessagesParameters.Add("MachineName", Environment.MachineName);

            await SafeSystemScan(MonitorEvents.SchedulerStarted, info, null, cancellationToken);
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
            return Task.CompletedTask;
        }

        public Task TriggerResumed(TriggerKey triggerKey, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task TriggersPaused(string triggerGroup, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task TriggersResumed(string triggerGroup, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        private static MonitorSystemInfo GetJobKeyMonitorSystemInfo(JobKey jobKey, string title)
        {
            var info = new MonitorSystemInfo
            {
                MessageTemplate = $"Job {{{{JobGroup}}}}.{{{{JobName}}}} was {title}"
            };

            info.MessagesParameters.Add("JobGroup", jobKey.Group);
            info.MessagesParameters.Add("JobName", jobKey.Name);
            return info;
        }
    }
}