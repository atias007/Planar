using Microsoft.Extensions.Logging;
using Planar.Service.General;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs;

public abstract class SystemJob
{
    protected const string LastRunKey = "Last.Success.Run";

    protected static IJobDetail CreateJob<T>(JobKey jobKey, string description)
        where T : IJob
    {
        var jobId = ServiceUtil.GenerateId();
        var job = JobBuilder.Create<T>()
            .WithIdentity(jobKey)
            .UsingJobData(Consts.JobId, jobId)
            .DisallowConcurrentExecution()
            .PersistJobDataAfterExecution()
            .WithDescription(description)
            .StoreDurably(true)
            .Build();

        return job;
    }

    protected static JobKey CreateJobKey<T>()
        where T : IJob
    {
        var name = typeof(T).Name;
        var jobKey = new JobKey(name, Consts.PlanarSystemGroup);
        return jobKey;
    }

    protected static void SafeSetLastRun(IJobExecutionContext context, ILogger logger)
    {
        try
        {
            context.JobDetail.JobDataMap.Put(LastRunKey, DateTime.Now.ToString());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "fail to set last run of system job");
        }
    }

    // For circuit breaker job, we need to ensure that the job is scheduled without any trigger.
    protected static async Task<JobKey> Schedule<T>(IScheduler scheduler, string description, CancellationToken stoppingToken = default)
        where T : IJob
    {
        var jobKey = CreateJobKey<T>();
        var job = await scheduler.GetJobDetail(jobKey, stoppingToken);
        if (job != null) { return jobKey; }
        job = CreateJob<T>(jobKey, description);
        var triggers = new List<ITrigger>();
        await scheduler.ScheduleJob(job, triggers, replace: true, stoppingToken);

        return jobKey;
    }

    protected static Task<JobKey> ScheduleHighPriority<T>(IScheduler scheduler, string description, string cronexpr, CancellationToken stoppingToken = default)
        where T : IJob
    {
        return Schedule<T>(scheduler, description, cronexpr, int.MaxValue, stoppingToken);
    }

    protected static Task<JobKey> ScheduleLowPriority<T>(IScheduler scheduler, string description, string cronexpr, CancellationToken stoppingToken = default)
        where T : IJob
    {
        return Schedule<T>(scheduler, description, cronexpr, int.MinValue, stoppingToken);
    }

    private static async Task<IJobDetail?> GetJobDetails(IScheduler scheduler, JobKey jobKey, string cronexpr, CancellationToken stoppingToken = default)
    {
        var job = await scheduler.GetJobDetail(jobKey, stoppingToken);
        if (job == null) { return null; }

        var triggers = await scheduler.GetTriggersOfJob(jobKey, stoppingToken);

        // job without trigger
        if (triggers.FirstOrDefault() is not ICronTrigger cronTrigger)
        {
            await scheduler.DeleteJob(jobKey, stoppingToken);
            return null;
        }

        // job with multiple trigger
        if (triggers.Count > 1)
        {
            await scheduler.DeleteJob(jobKey, stoppingToken);
            return null;
        }

        // job with faulted trigger
        if (await scheduler.GetTriggerState(cronTrigger.Key, stoppingToken) == TriggerState.Error)
        {
            await scheduler.DeleteJob(jobKey, stoppingToken);
            return null;
        }

        // same cron expression
        if (string.Equals(cronTrigger.CronExpressionString, cronexpr, StringComparison.OrdinalIgnoreCase)) { return job; }

        await scheduler.DeleteJob(jobKey, stoppingToken);
        return null;
    }

    private static async Task<JobKey> Schedule<T>(IScheduler scheduler, string description, string cronexpr, int priority, CancellationToken stoppingToken)
        where T : IJob
    {
        var jobKey = CreateJobKey<T>();
        var job = await GetJobDetails(scheduler, jobKey, cronexpr, stoppingToken);
        if (job != null) { return jobKey; }
        job = CreateJob<T>(jobKey, description);

        var triggerId = ServiceUtil.GenerateId();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(jobKey.Name, jobKey.Group)
            .UsingJobData(Consts.TriggerId, triggerId)
            .WithCronSchedule(cronexpr, b => b.WithMisfireHandlingInstructionFireAndProceed())
            .WithPriority(priority)
            .Build();

        await scheduler.ScheduleJob(job, [trigger], true, stoppingToken);

        return jobKey;
    }
}