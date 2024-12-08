using CommonJob;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Audit;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Listeners.Base;
using Planar.Service.Monitor;
using Quartz;
using System;
using System.Collections.Generic;
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
                    var info = new Dictionary<string, object?>
                    {
                        { "failure threshold", cb.FailureThreshold },
                        { "pause span", cb.PauseSpan },
                    };
                    AuditJobSafe(context.JobDetail.Key, "system paused job due to circuit breaker", info);
                    RaiseAlert(context);
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

    public void RaiseAlert(IJobExecutionContext context)
    {
        var info = new MonitorSystemInfo
        (
               "Circuit breaker was activated for job {{JobGroup}}.{{JobName}} with description {{Description}}"
        );

        info.MessagesParameters.Add("JobGroup", context.JobDetail.Key.Group);
        info.MessagesParameters.Add("JobName", context.JobDetail.Key.Name);
        info.MessagesParameters.Add("Description", context.JobDetail.Description);
        info.AddMachineName();
        SafeSystemScan(MonitorEvents.CircuitBreakerActivated, info);
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
        try
        {
            await AutoResumeJobUtil.QueueResumeJob(context.Scheduler, context.JobDetail, cb.PauseSpan.Value, AutoResumeTypes.CircuitBreaker);
        }
        catch (JobNotFoundException ex)
        {
            _logger.LogCritical(ex, "Job with key '{Key}' was not found while try to queue circuit breaker resume", ex.Key);
        }
    }
}