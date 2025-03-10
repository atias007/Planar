﻿using Microsoft.Extensions.Logging;
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

internal class CircuitBreakerJob(
    ILogger<CircuitBreakerJob> logger,
    IScheduler scheduler,
    AuditProducer auditProducer,
    MonitorUtil monitorUtil) : SystemJob, IJob
{
    public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
    {
        const string description = "system job for resume job running after circuit breaker/auto resume";
        await Schedule<CircuitBreakerJob>(scheduler, description, stoppingToken: stoppingToken);
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await DoWork(context);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "fail to resume from circuit breaker/auto resume");
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

    private async Task DoWork(IJobExecutionContext context)
    {
        // Get resume type
        var resumeTypeText = context.Trigger.JobDataMap.GetString(AutoResumeJobUtil.ResumeType);
        if (!Enum.TryParse<AutoResumeTypes>(resumeTypeText, out var resumeType)) { resumeType = AutoResumeTypes.AutoResume; }

        // Get destination job key
        var jobKeyName = context.Trigger.JobDataMap.GetString(AutoResumeJobUtil.JobKeyName);
        var jobKeyGroup = context.Trigger.JobDataMap.GetString(AutoResumeJobUtil.JobKeyGroup);
        if (string.IsNullOrWhiteSpace(jobKeyName)) { return; }
        if (string.IsNullOrWhiteSpace(jobKeyGroup)) { return; }
        var jobKey = new JobKey(jobKeyName, jobKeyGroup);

        // Resume job (in case of auto resume)
        if (resumeType == AutoResumeTypes.AutoResume)
        {
            await scheduler.ResumeJob(jobKey, context.CancellationToken);
            AuditJobSafe(jobKey, "auto resume job");
            return;
        }

        // Resume job (in case of circuit breaker)
        // 1. Get destination trigger names
        var triggerGroup = context.Trigger.JobDataMap.GetString(AutoResumeJobUtil.TriggerGroup);
        var triggerNamesText = context.Trigger.JobDataMap.GetString(AutoResumeJobUtil.TriggerNames);
        if (string.IsNullOrWhiteSpace(triggerGroup)) { return; }
        if (string.IsNullOrWhiteSpace(triggerNamesText)) { return; }
        var triggerNames = triggerNamesText.Split(',');
        if (triggerNames.Length == 0) { return; }

        // 2. Get current job triggers
        var triggers = await scheduler.GetTriggersOfJob(jobKey, context.CancellationToken);

        // 3. Resume triggers
        foreach (var name in triggerNames)
        {
            var trigger = triggers.FirstOrDefault(t => t.Key.Name == name && t.Key.Group == triggerGroup);
            if (trigger == null) { continue; }

            var triggerKey = new TriggerKey(name, triggerGroup);
            var state = await scheduler.GetTriggerState(triggerKey, context.CancellationToken);
            if (TriggerHelper.IsActiveState(state)) { continue; }

            await scheduler.ResumeTrigger(triggerKey, context.CancellationToken);
        }

        AuditJobSafe(jobKey, "auto resume job after circuit breaker reset");
        await SafeScan(MonitorEvents.CircuitBreakerReset, context.Scheduler, jobKey);
    }

    private async Task SafeScan(MonitorEvents @event, IScheduler scheduler, JobKey jobKey)
    {
        try
        {
            var jobDetails = await scheduler.GetJobDetail(jobKey);
            if (jobDetails == null) { return; }
            var info = new MonitorSystemInfo
            (
                "Circuit breaker was reset for job {{JobGroup}}.{{JobName}} with description {{Description}}"
            );

            info.MessagesParameters.Add("JobGroup", jobDetails.Key.Group);
            info.MessagesParameters.Add("JobName", jobDetails.Key.Name);
            info.MessagesParameters.Add("Description", jobDetails.Description);
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