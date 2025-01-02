using Microsoft.Extensions.Logging;
using Planar.Service.General;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Quartz.MisfireInstruction;

namespace Planar.Service.SystemJobs;

public abstract class SystemJob
{
    protected const string LastRunKey = "Last.Success.Run";

    protected static IJobDetail CreateJob<T>(JobKey jobKey, string description)
        where T : IJob
    {
        var jobId = ServiceUtil.GenerateId();
        var job = JobBuilder.Create(typeof(T))
            .WithIdentity(jobKey)
            .UsingJobData(Consts.JobId, jobId)
            .DisallowConcurrentExecution()
            .PersistJobDataAfterExecution()
            .WithDescription(description)
            .StoreDurably(true)
            .Build();

        return job;
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

    protected static JobKey CreateJobKey<T>()
        where T : IJob
    {
        var name = typeof(T).Name;
        var jobKey = new JobKey(name, Consts.PlanarSystemGroup);
        return jobKey;
    }

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

    protected static async Task<JobKey> Schedule<T>(IScheduler scheduler, string description, TimeSpan span, DateTime? startDate = null, CancellationToken stoppingToken = default)
                    where T : IJob
    {
        var jobKey = CreateJobKey<T>();
        var job = await GetJobDetails(scheduler, jobKey, span, startDate, stoppingToken);
        if (job != null) { return jobKey; }
        job = CreateJob<T>(jobKey, description);

        var triggerId = ServiceUtil.GenerateId();
        DateTimeOffset jobStart;
        if (startDate == null)
        {
            jobStart = new DateTimeOffset(DateTime.Now);
            jobStart = jobStart.AddSeconds(-jobStart.Second);
            jobStart = jobStart.Add(span);
        }
        else
        {
            jobStart = new DateTimeOffset(startDate.Value);
        }

        var trigger = TriggerBuilder.Create()
            .WithIdentity(jobKey.Name, jobKey.Group)
            .StartAt(jobStart)
            .UsingJobData(Consts.TriggerId, triggerId)
            .WithSimpleSchedule(s => BuildSimpleSchedule(s, span))
            .WithPriority(int.MinValue)
            .Build();

        await scheduler.ScheduleJob(job, [trigger], true, stoppingToken);

        return jobKey;
    }

    private static async Task<IJobDetail?> GetJobDetails(IScheduler scheduler, JobKey jobKey, TimeSpan triggerInterval, DateTime? startDate, CancellationToken stoppingToken = default)
    {
        var job = await scheduler.GetJobDetail(jobKey, stoppingToken);
        if (job == null) { return null; }

        var triggers = await scheduler.GetTriggersOfJob(jobKey, stoppingToken);
        if (triggers.FirstOrDefault() is not ISimpleTrigger simpleTrigger) // job without trigger
        {
            await scheduler.DeleteJob(jobKey, stoppingToken);
            return null;
        }

        var existsInterval = Convert.ToInt32(Math.Floor(simpleTrigger.RepeatInterval.TotalSeconds));
        var currentInterval = Convert.ToInt32(Math.Floor(triggerInterval.TotalSeconds));
        if (startDate == null && existsInterval == currentInterval) { return job; }

        var existsStartDate = NormalizeDateTime(simpleTrigger.StartTimeUtc.DateTime);
        var currentStartDate = NormalizeDateTime(startDate.GetValueOrDefault().ToUniversalTime());

        var diff = Math.Abs((existsStartDate - currentStartDate).TotalSeconds);
        if (existsInterval == currentInterval && diff <= 60) { return job; }
        await scheduler.DeleteJob(jobKey, stoppingToken);
        return null;
    }

    private static DateTime NormalizeDateTime(DateTime date)
    {
        return DateTime.Now.Date
            .AddHours(date.Hour)
            .AddMinutes(date.Minute)
            .AddSeconds(date.Second);
    }

    private static void BuildSimpleSchedule(SimpleScheduleBuilder builder, TimeSpan span)
    {
        builder
        .WithInterval(span)
        .RepeatForever();

        if (span.TotalMinutes > 15)
        {
            builder.WithMisfireHandlingInstructionFireNow();
        }
        else
        {
            builder.WithMisfireHandlingInstructionNextWithExistingCount();
        }
    }
}