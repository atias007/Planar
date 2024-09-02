using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.Audit;
using Planar.Service.General;
using Planar.Service.Monitor;
using Quartz;
using Quartz.Util;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs;

internal class CircuitBreakerJob(ILogger<CircuitBreakerJob> logger, IScheduler scheduler, AuditProducer auditProducer, MonitorUtil monitorUtil)
    : SystemJob, IJob
{
    public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
    {
        const string description = "system job for resume job to run after circuit breaker";
        await Schedule<CircuitBreakerJob>(scheduler, description, stoppingToken: stoppingToken);
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await DoWork(context);
            AuditJobSafe(context.JobDetail.Key, "system resume job after circuit breaker activation");
            SafeScan(MonitorEvents.CircuitBreakerReset, context);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "fail to resume from circuit breaker");
        }
    }

    private static T? Parse<T>(string value) where T : Key<T>
    {
        var index = value.IndexOf('.');
        if (index < 0) { return null; }
        var group = value[..index];
        var name = value[(index + 1)..];
        var result = Activator.CreateInstance(typeof(T), name, group) as T;
        return result;
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
            auditProducer.Publish(audit);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to publish job/trigger audit message. the message: {@Message}", audit);
        }
    }

    private async Task<bool> DoWork(IJobExecutionContext context)
    {
        var jobKeyText = context.Trigger.JobDataMap.GetString("JobKey");
        if (string.IsNullOrWhiteSpace(jobKeyText)) { return false; }
        var jobKey = Parse<JobKey>(jobKeyText);
        if (jobKey == null) { return false; }

        var triggerData = context.Trigger.JobDataMap.GetString(Consts.CircuitBreakerJobDataKey);
        if (string.IsNullOrWhiteSpace(triggerData)) { return false; }

        var keys = triggerData.Split(',');
        if (keys.Length == 0) { return false; }

        var triggers = await scheduler.GetTriggersOfJob(jobKey, context.CancellationToken);
        var result = false;

        foreach (var item in keys)
        {
            var triggerKey = Parse<TriggerKey>(item);
            if (triggerKey == null) { continue; }

            var trigger = triggers.FirstOrDefault(t => t.Key.Name == triggerKey.Name && t.Key.Group == triggerKey.Group);
            if (trigger == null) { continue; }

            var state = await scheduler.GetTriggerState(triggerKey, context.CancellationToken);
            if (TriggerHelper.IsActiveState(state)) { continue; }

            await scheduler.ResumeTrigger(triggerKey, context.CancellationToken);
            result = true;
        }

        return result;
    }

    private void SafeScan(MonitorEvents @event, IJobExecutionContext context)
    {
        try
        {
            var info = new MonitorSystemInfo
            (
                "Circuit breaker was reset for job {{JobGroup}}.{{JobName}} with description {{Description}}"
            );

            info.MessagesParameters.Add("JobGroup", context.JobDetail.Key.Group);
            info.MessagesParameters.Add("JobName", context.JobDetail.Key.Name);
            info.MessagesParameters.Add("Description", context.JobDetail.Description);
            info.AddMachineName();

            monitorUtil.Scan(@event, info);
        }
        catch (ObjectDisposedException)
        {
            ServiceUtil.AddDisposeWarningToLog(logger);
        }
        catch (Exception ex)
        {
            var source = nameof(SafeScan);
            logger.LogCritical(ex, "Error handle {Source}: {Message}", source, ex.Message);
        }
    }
}