using CommonJob;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Planar.Service.Audit;
using Planar.Service.General;
using Planar.Service.Listeners.Base;
using Planar.Service.SystemJobs;
using Quartz;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Listeners;

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
            var cb = JobHelper.GetJobCircuitBreaker(context.JobDetail);
            if (cb == null) { return; }

            var unhadleException = JobExecutionMetadata.GetInstance(context)?.UnhandleException;
            var executionException = unhadleException ?? jobException;
            var status = executionException == null ? StatusMembers.Success : StatusMembers.Fail;

            if (status == StatusMembers.Success)
            {
                if (cb.SuccessCounter == 0 && cb.FailCounter == 0) { return; }
                cb.SuccessCounter++;
                if (cb.SuccessCounter >= cb.SuccessThreshold)
                {
                    cb.Reset();
                }

                SaveCircuitBreaker(context, cb);
            }
            else
            {
                cb.FailCounter++;
                if (cb.FailCounter >= cb.FailureThreshold)
                {
                    await QueueResumeJob(context, cb);
                    await PauseJob(context);
                    AuditJobSafe(context.JobDetail.Key, "system paused job due to circuit breaker", new { cb.FailureThreshold, cb.PauseSpan });
                    SafeScan(MonitorEvents.CircuitBreakerActivated, context);
                    cb.Reset();
                }

                SaveCircuitBreaker(context, cb);
            }
        }
        catch (Exception ex)
        {
            LogCritical(nameof(JobToBeExecuted), ex);
        }
    }

    private static void SaveCircuitBreaker(IJobExecutionContext context, JobCircuitBreakerMetadata circuitBreaker)
    {
        var cbText = circuitBreaker.ToString();
        context.JobDetail.JobDataMap.Put(Consts.CircuitBreaker, cbText);
    }

    private static async Task PauseJob(IJobExecutionContext context)
    {
        var jobKey = context.JobDetail.Key;
        await context.Scheduler.PauseJob(jobKey);
    }

    private void AuditJobSafe(JobKey jobKey, string description, object? additionalInfo = null)
    {
        var audit = new AuditMessage
        {
            JobKey = jobKey,
            Description = description,
            AdditionalInfo = additionalInfo
        };

        try
        {
            using var scope = ServiceScopeFactory.CreateScope();
            var producer = scope.ServiceProvider.GetRequiredService<AuditProducer>();
            producer.Publish(audit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to publish job/trigger audit message. the message: {@Message}", audit);
        }
    }

    private async Task QueueResumeJob(IJobExecutionContext context, JobCircuitBreakerMetadata cb)
    {
        if (cb.PauseSpan == null) { return; }

        var jobKey = new JobKey(typeof(CircuitBreakerJob).Name, Consts.PlanarSystemGroup);
        var job = await context.Scheduler.GetJobDetail(jobKey);
        if (job == null)
        {
            _logger.LogCritical("Job with name 'CircuitBreakerJob' was not found while try to queue circuit breaker resume");
            return;
        }

        var triggers = await context.Scheduler.GetTriggersOfJob(context.JobDetail.Key);
        var triggersStates = triggers.Select(async t => new { t.Key, State = await context.Scheduler.GetTriggerState(t.Key) });
        var activeTriggers = triggersStates.Where(t => TriggerHelper.IsActiveState(t.Result.State)).Select(t => t.Result.Key.ToString());
        var activeTriggerValue = string.Join(',', activeTriggers);

        var triggerKey = new TriggerKey($"Resume.{context.JobDetail.Key}", Consts.CircuitBreakerTriggerGroup);
        var triggerId = ServiceUtil.GenerateId();
        var key = context.JobDetail.Key.ToString();
        var dueDate = DateTime.Now.Add(cb.PauseSpan.Value);
        var newTrigger = TriggerBuilder.Create()
             .WithIdentity(triggerKey)
             .UsingJobData(Consts.TriggerId, triggerId)
             .UsingJobData("JobKey", key)
             .UsingJobData(Consts.CircuitBreakerJobDataKey, activeTriggerValue)
             .StartAt(dueDate)
             .WithSimpleSchedule(b =>
             {
                 b.WithRepeatCount(0)
                 .WithMisfireHandlingInstructionFireNow();
             })
             .ForJob(job);

        // Schedule Job
        await context.Scheduler.ScheduleJob(newTrigger.Build());
    }
}