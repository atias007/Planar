using CommonJob;
using Microsoft.Extensions.Logging;
using Planner.Common;
using Planner.Service.General;
using Planner.Service.JobListener.Base;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planner.Service.JobListener
{
    public class RetryTriggerListener : BaseListener<RetryTriggerListener>, ITriggerListener
    {
        public string Name => nameof(RetryTriggerListener);

        public async Task TriggerComplete(ITrigger trigger, IJobExecutionContext context, SchedulerInstruction triggerInstructionCode, CancellationToken cancellationToken = default)
        {
            try
            {
                if (context.JobDetail.Key.Group == Consts.PlannerSystemGroup) { return; }

                if (context.JobInstance is ICommonJob job && job.GetJobRunningProperty<bool>("Fail") == false) { return; }
                if (trigger.JobDataMap.Contains(Consts.RetrySpan) == false) { return; }
                var span = GetRetrySpan(trigger);
                if (span == null) { return; }

                var numTries = trigger.JobDataMap.GetIntValue(Consts.RetryCounter);
                if (numTries > Consts.MaxRetries)
                {
                    Logger.Log(LogLevel.Error, $"Job with key {context.JobDetail.Key.Group}.{context.JobDetail.Key.Name} fail and retry for {Consts.MaxRetries} times but failed each time.");
                    return;
                }

                var id = Convert.ToString(trigger.JobDataMap[Consts.TriggerId]);
                var name = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString().Replace("-", string.Empty) : id;
                var start = DateTime.Now.AddSeconds(span.Value.TotalSeconds);
                var retryTrigger = TriggerBuilder
                        .Create()
                        .ForJob(context.JobDetail)
                        .WithIdentity($"RetryCount_{numTries}_{name}", Consts.RetryTriggerGroup)
                        .UsingJobData(Consts.TriggerId, ServiceUtil.GenerateId())
                        .UsingJobData(Consts.RetrySpan, span.GetValueOrDefault().ToSimpleTimeString())
                        .UsingJobData(Consts.RetryCounter, numTries)
                        .StartAt(start)
                        .Build();

                if (numTries > 1)
                {
                    Logger.Log(LogLevel.Warning, $"Retry no. {numTries} of job with key {context.JobDetail.Key.Group}.{context.JobDetail.Key.Name} was fail. Retry again at {start}.");
                }
                else
                {
                    Logger.Log(LogLevel.Warning, $"Job with key {context.JobDetail.Key.Group}.{context.JobDetail.Key.Name} was fail. Retry again at {start}.");
                }

                await context.Scheduler.ScheduleJob(retryTrigger, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, ex, $"Error handle '{nameof(TriggerComplete)}' at '{nameof(RetryTriggerListener)}'");
            }
        }

        public async Task TriggerFired(ITrigger trigger, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (context.JobDetail.Key.Group == Consts.PlannerSystemGroup) { return; }
                if (trigger.JobDataMap.Contains(Consts.RetrySpan) == false) { return; }
                var span = GetRetrySpan(trigger);
                if (span == null) { return; }

                if (!trigger.JobDataMap.Contains(Consts.RetryCounter))
                {
                    trigger.JobDataMap.Put(Consts.RetryCounter, 0);
                }

                var numberTries = trigger.JobDataMap.GetIntValue(Consts.RetryCounter);
                trigger.JobDataMap.Put(Consts.RetryCounter, ++numberTries);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error handle '{nameof(TriggerFired)}' at '{nameof(RetryTriggerListener)}'", ex);
            }
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
            if (TimeSpan.TryParse(spanValue, out TimeSpan span) == false) { return null; }
            return span;
        }
    }
}