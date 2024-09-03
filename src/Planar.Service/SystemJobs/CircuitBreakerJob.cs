using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.Audit;
using Planar.Service.General;
using Planar.Service.Monitor;
using Quartz;
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
        // Get destination job key
        var jobKeyName = context.Trigger.JobDataMap.GetString("JobKey.Name");
        var jobKeyGroup = context.Trigger.JobDataMap.GetString("JobKey.Group");
        if (string.IsNullOrWhiteSpace(jobKeyName)) { return false; }
        if (string.IsNullOrWhiteSpace(jobKeyGroup)) { return false; }
        var jobKey = new JobKey(jobKeyName, jobKeyGroup);

        // Get destination trigger names
        var triggerGroup = context.Trigger.JobDataMap.GetString("Trigger.Group");
        var triggerNamesText = context.Trigger.JobDataMap.GetString("Trigger.Names");
        if (string.IsNullOrWhiteSpace(triggerGroup)) { return false; }
        if (string.IsNullOrWhiteSpace(triggerNamesText)) { return false; }
        var triggerNames = triggerNamesText.Split(',');
        if (triggerNames.Length == 0) { return false; }

        // Get current job triggers
        var triggers = await scheduler.GetTriggersOfJob(jobKey, context.CancellationToken);
        var result = false;

        // Resume all triggers
        foreach (var name in triggerNames)
        {
            var trigger = triggers.FirstOrDefault(t => t.Key.Name == name && t.Key.Group == triggerGroup);
            if (trigger == null) { continue; }

            var triggerKey = new TriggerKey(name, triggerGroup);
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