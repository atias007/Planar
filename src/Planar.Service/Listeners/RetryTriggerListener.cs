using CommonJob;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.General;
using Planar.Service.Listeners.Base;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Listeners
{
    public class RetryTriggerListener : BaseListener<RetryTriggerListener>, ITriggerListener
    {
        public RetryTriggerListener(IServiceScopeFactory serviceScopeFactory, ILogger<RetryTriggerListener> logger) : base(serviceScopeFactory, logger)
        {
        }

        public string Name => nameof(RetryTriggerListener);

        public Task TriggerComplete(ITrigger trigger, IJobExecutionContext context, SchedulerInstruction triggerInstructionCode, CancellationToken cancellationToken = default)
        {
            try
            {
                if (IsSystemTrigger(trigger)) { return Task.CompletedTask; }
                if (IsSystemJob(context.JobDetail)) { return Task.CompletedTask; }

                var metadata = JobExecutionMetadata.GetInstance(context);
                if (metadata.IsRunningSuccess) { return Task.CompletedTask; }
                if (!trigger.JobDataMap.Contains(Consts.RetrySpan)) { return Task.CompletedTask; }
                var span = GetRetrySpan(trigger);
                if (span == null) { return Task.CompletedTask; }

                var numTries = trigger.JobDataMap.GetIntValue(Consts.RetryCounter);
                if (numTries > Consts.MaxRetries)
                {
                    var key = $"{context.JobDetail.Key.Group}.{context.JobDetail.Key.Name}";
                    _logger.LogError("Job with key {Key} fail and retry for {MaxRetries} times but failed each time", key, Consts.MaxRetries);
                    return SafeScan(MonitorEvents.ExecutionLastRetryFail, context);
                }

                var id = TriggerKeyHelper.GetTriggerId(trigger);
                var name = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString().Replace("-", string.Empty) : id;
                var start = DateTime.Now.AddSeconds(span.Value.TotalSeconds);
                var retryTrigger = TriggerBuilder
                        .Create()
                        .ForJob(context.JobDetail)
                        .WithIdentity($"{Consts.RetryTriggerNamePrefix}_{numTries}_{name}", Consts.RetryTriggerGroup)
                        .UsingJobData(Consts.TriggerId, ServiceUtil.GenerateId())
                        .UsingJobData(Consts.RetrySpan, span.GetValueOrDefault().ToSimpleTimeString())
                        .UsingJobData(Consts.RetryCounter, numTries.ToString())
                        .StartAt(start)
                        .Build();

                if (numTries > 1)
                {
                    var key = $"{context.JobDetail.Key.Group}.{context.JobDetail.Key.Name}";
                    _logger.LogWarning("Retry no. {NumTries} of job with key {Key} was fail. Retry again at {Start}", numTries, key, start);
                }
                else
                {
                    var key = $"{context.JobDetail.Key.Group}.{context.JobDetail.Key.Name}";
                    _logger.LogWarning("Job with key {Key} was fail. Retry again at {Start}", key, start);
                }

                context.Scheduler.ScheduleJob(retryTrigger, cancellationToken).Wait(cancellationToken);
                return SafeScan(MonitorEvents.ExecutionRetry, context);
            }
            catch (Exception ex)
            {
                LogCritical(nameof(TriggerComplete), ex);
                return Task.CompletedTask;
            }
        }

        public Task TriggerFired(ITrigger trigger, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (IsSystemTrigger(trigger)) { return Task.CompletedTask; }
                if (IsSystemJob(context.JobDetail)) { return Task.CompletedTask; }

                if (!trigger.JobDataMap.Contains(Consts.RetrySpan)) { return Task.CompletedTask; }
                var span = GetRetrySpan(trigger);
                if (span == null) { return Task.CompletedTask; }

                if (!trigger.JobDataMap.Contains(Consts.RetryCounter))
                {
                    trigger.JobDataMap.Put(Consts.RetryCounter, 0);
                }

                var numberTries = trigger.JobDataMap.GetIntValue(Consts.RetryCounter);
                numberTries++;
                trigger.JobDataMap.Put(Consts.RetryCounter, numberTries);
            }
            catch (Exception ex)
            {
                LogCritical(nameof(TriggerFired), ex);
            }

            return Task.CompletedTask;
        }

        public Task TriggerMisfired(ITrigger trigger, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> VetoJobExecution(ITrigger trigger, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        private static TimeSpan? GetRetrySpan(ITrigger trigger)
        {
            var value = trigger.JobDataMap[Consts.RetrySpan];
            var spanValue = Convert.ToString(value);
            if (string.IsNullOrEmpty(spanValue)) { return null; }
            if (!TimeSpan.TryParse(spanValue, out TimeSpan span)) { return null; }
            return span;
        }
    }
}