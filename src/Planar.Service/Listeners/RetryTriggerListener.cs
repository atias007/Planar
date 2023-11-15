using CommonJob;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Helpers;
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
            return Task.Run(() =>
                {
                    TriggerCompleteInner(trigger, context, cancellationToken);
                }, cancellationToken);
        }

        public Task TriggerFired(ITrigger trigger, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
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

        private void TriggerCompleteInner(ITrigger trigger, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Ignore system job / trigger
                if (IsSystemTrigger(trigger)) { return; }
                if (IsSystemJob(context.JobDetail)) { return; }

                // Ignore success running
                var metadata = JobExecutionMetadata.GetInstance(context);
                if (metadata.IsRunningSuccess) { return; }

                // Ignore triggers with no retry
                if (!TriggerHelper.HasRetry(trigger)) { return; }

                // Ignore trigger with no trigger span
                var span = TriggerHelper.GetRetrySpan(trigger);
                if (span == null) { return; }

                // Get retry counters
                var numTries = TriggerHelper.GetRetryNumber(trigger) ?? 0;
                var maxRetries = TriggerHelper.GetMaxRetriesWithDefault(context.Trigger);

                // Last retry - No more retries
                var key = JobHelper.GetKeyTitle(context.JobDetail);
                if (numTries >= maxRetries)
                {
                    _logger.LogError("job with key {Key} fail and retry for {NumberTries} times but failed each time", key, numTries);
                    SafeScan(MonitorEvents.ExecutionLastRetryFail, context);
                    return;
                }

                // Calculate the next start retry
                var start = DateTime.Now.AddSeconds(span.Value.TotalSeconds);

                // Log as warning the retry details
                if (numTries > 0)
                {
                    _logger.LogWarning("retry no. {NumTries} of job with key {Key} was fail. Retry again at {Start}", numTries, key, start);
                }
                else
                {
                    _logger.LogWarning("job with key {Key} was fail. Retry again at {Start}", key, start);
                }

                numTries++;
                trigger.JobDataMap.Put(Consts.RetryCounter, numTries.ToString());

                var id = TriggerHelper.GetTriggerId(trigger);
                var name = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString().Replace("-", string.Empty) : id;
                var retryTrigger = TriggerBuilder
                        .Create()
                        .ForJob(context.JobDetail)
                        .WithIdentity($"{Consts.RetryTriggerNamePrefix}.{numTries}.{name}", Consts.RetryTriggerGroup)
                        .UsingJobData(Consts.TriggerId, ServiceUtil.GenerateId())
                        .UsingJobData(Consts.RetrySpan, span.GetValueOrDefault().ToSimpleTimeString())
                        .UsingJobData(Consts.RetryCounter, numTries.ToString())
                        .UsingJobData(Consts.MaxRetries, maxRetries.ToString())
                        .StartAt(start)
                        .Build();

                context.Scheduler.ScheduleJob(retryTrigger, cancellationToken).Wait(cancellationToken);
                SafeScan(MonitorEvents.ExecutionRetry, context);
            }
            catch (Exception ex)
            {
                LogCritical(nameof(TriggerComplete), ex);
            }
        }
    }
}