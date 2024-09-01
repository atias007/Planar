using CommonJob;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.API.Helpers;
using Planar.Service.Listeners.Base;
using Quartz;
using Quartz.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using static Quartz.Logging.OperationName;

namespace Planar.Service.Listeners
{
    internal class CircuitBreakerJobListener(IServiceScopeFactory serviceScopeFactory, ILogger<CircuitBreakerJobListener> logger)
        : BaseListener<CircuitBreakerJobListener>(serviceScopeFactory, logger), IJobListener
    {
        public string Name => nameof(CircuitBreakerJobListener);

        public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken cancellationToken = default)
        {
            try
            {
                if (IsSystemJob(context.JobDetail)) { return; }

                var unhadleException = JobExecutionMetadata.GetInstance(context)?.UnhandleException;
                var executionException = unhadleException ?? jobException;
                var status = executionException == null ? StatusMembers.Success : StatusMembers.Fail;
                var cb = JobHelper.GetJobCircuitBreaker(context.JobDetail);
                if (cb == null) { return; }
                if (status == StatusMembers.Success)
                {
                    cb.SuccessCounter++;
                    if (cb.SuccessCounter >= cb.SuccessThreshold)
                    {
                        cb.Reset();
                    }

                    SaveCircleBreaker(context, cb);
                }
                else
                {
                    cb.FailCounter++;
                    if (cb.FailCounter >= cb.FailureThreshold)
                    {
                        await PauseJob(context);
                        await QueueResumeJob(context);
                        cb.Reset();
                    }

                    SaveCircleBreaker(context, cb);
                }
            }
            catch (Exception ex)
            {
                LogCritical(nameof(JobToBeExecuted), ex);
            }
        }

        private static void SaveCircleBreaker(IJobExecutionContext context, JobCircuitBreakerMetadata circuitBreaker)
        {
            var cbText = circuitBreaker.ToString();
            context.JobDetail.JobDataMap.Put(Consts.CircuitBreaker, cbText);
        }

        private static async Task PauseJob(IJobExecutionContext context)
        {
            var jobKey = context.JobDetail.Key;
            await context.Scheduler.PauseJob(jobKey);
        }

        private async Task QueueResumeJob(IJobExecutionContext context, JobCircuitBreakerMetadata cb)
        {
            if (cb.PauseSpan == null) { return; }

            var jobKey = new JobKey(Consts.CircuitBreakerJobName, Consts.PlanarSystemGroup);
            var job = await context.Scheduler.GetJobDetail(jobKey);
            if (job == null)
            {
                _logger.LogCritical("CircuitBreakerJob not found while try to queue resume");
                return;
            }

            var dueDate = DateTime.Now.Add(cb.PauseSpan.Value);
            var newTrigger = TriggerBuilder.Create()
                 .WithIdentity(triggerKey)
                 .UsingJobData(Consts.TriggerId, triggerId)
                 .StartAt(dueDate)
                 .WithSimpleSchedule(b =>
                 {
                     b.WithRepeatCount(0).WithMisfireHandlingInstructionFireNow();
                 })
                 .ForJob(job);

            // Schedule Job
            await context.Scheduler.ScheduleJob(newTrigger.Build());
        }
    }
}