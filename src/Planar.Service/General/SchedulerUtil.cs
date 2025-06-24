using CommonJob;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Planar.Service.Exceptions;
using Planar.Service.Model.DataObjects;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.General;

public class SchedulerUtil(IScheduler scheduler, SchedulerHealthCheckUtil schedulerHealthCheckUtil, JobKeyHelper jobKeyHelper)
{
    public IScheduler Scheduler => scheduler;

    internal string SchedulerInstanceId
    {
        get
        {
            return scheduler.SchedulerInstanceId;
        }
    }

    public async Task Start(CancellationToken cancellationToken = default)
    {
        await scheduler.Start(cancellationToken);
    }

    public async Task Shutdown(CancellationToken cancellationToken = default)
    {
        await scheduler.Shutdown(true, cancellationToken);
    }

    public async Task Stop(CancellationToken cancellationToken = default)
    {
        await scheduler.Standby(cancellationToken);
    }

    public async Task<ITrigger?> GetCircuitBreakerTrigger(JobKey jobKey)
    {
        var triggerKey = AutoResumeJobUtil.GetResumeTriggerKey(jobKey);
        var trigger = await scheduler.GetTrigger(triggerKey);
        return trigger;
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            // Check if the scheduler is started and running
            if (!IsSchedulerRunning) { return false; }

            // Check if the last run was within a reasonable time frame (e.g., 5 minutes)
            var timeSinceLastRun = DateTimeOffset.UtcNow - schedulerHealthCheckUtil.LastRun;
            if (timeSinceLastRun.TotalMinutes < 5) { return true; }

            var running = await CountRunningJobs();
            var maxConcurrency = AppSettings.General.MaxConcurrency;
            if (running < maxConcurrency) { return false; }
            return true;
        }
        catch
        {
            // If any exception occurs, consider the scheduler unhealthy
            return false;
        }
    }

    public async Task HealthCheck(ILogger? logger = null)
    {
        if (!await IsHealthyAsync())
        {
            logger?.LogError("HealthCheck fail. IsShutdown={IsShutdown}, InStandbyMode={InStandbyMode}, IsStarted={IsStarted}",
                scheduler.IsShutdown, scheduler.InStandbyMode, scheduler.IsStarted);

            throw new PlanarException("scheduler is not running");
        }
    }

    public bool IsSchedulerRunning
    {
        get
        {
            return !scheduler.IsShutdown && !scheduler.InStandbyMode && scheduler.IsStarted;
        }
    }

    public async Task<bool> IsJobRunning(JobKey jobKey, CancellationToken cancellationToken = default)
    {
        var allRunning = await scheduler.GetCurrentlyExecutingJobs(cancellationToken);
        var result = allRunning.AsQueryable().Any(c => KeyHelper.Equals(c.JobDetail.Key, jobKey));
        return result;
    }

    public async Task<bool> IsTriggerRunning(TriggerKey triggerKey, CancellationToken cancellationToken = default)
    {
        var allRunning = await scheduler.GetCurrentlyExecutingJobs(cancellationToken);
        var result = allRunning.AsQueryable().Any(c => c.Trigger.Key.Equals(triggerKey));
        return result;
    }

    public async Task<int> CountRunningJobs(CancellationToken cancellationToken = default)
    {
        var jobs = await scheduler.GetCurrentlyExecutingJobs(cancellationToken);
        return jobs.Count;
    }

    public async Task<RunningJobDetails?> GetRunningJob(string instanceId, CancellationToken cancellationToken = default)
    {
        var jobs = await scheduler.GetCurrentlyExecutingJobs(cancellationToken);
        var context = jobs.FirstOrDefault(j => j.FireInstanceId == instanceId);
        if (context == null) { return null; }
        var details = new RunningJobDetails();
        MapJobRowDetails(context.JobDetail, details);
        MapJobExecutionContext(context, details);
        return details;
    }

    public async Task<List<RunningJobDetails>> GetRunningJobs(CancellationToken cancellationToken = default)
    {
        var result = new List<RunningJobDetails>();
        var jobs = await scheduler.GetCurrentlyExecutingJobs(cancellationToken);

        foreach (var context in jobs)
        {
            var details = new RunningJobDetails();
            MapJobRowDetails(context.JobDetail, details);
            MapJobExecutionContext(context, details);
            result.Add(details);
        }

        var response = result.OrderBy(r => r.Name).ToList();
        return response;
    }

    public async Task<RunningJobData?> GetRunningData(string instanceId, CancellationToken cancellationToken = default)
    {
        var context = (await scheduler.GetCurrentlyExecutingJobs(cancellationToken))
            .FirstOrDefault(j => j.FireInstanceId == instanceId);

        if (context == null) { return null; }

        var log = string.Empty;
        var exceptions = string.Empty;
        var count = 0;

        if (context.Result is JobExecutionMetadata metadata)
        {
            log = metadata.GetLogText();
            exceptions = metadata.GetExceptionsText();
            count = metadata.Exceptions.Count();
        }

        var response = new RunningJobData
        {
            Log = log,
            Exceptions = exceptions,
            ExceptionsCount = count
        };

        return response;
    }

    public async Task<bool> IsRunningInstanceExistOnLocal(string instanceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(instanceId))
        {
            return false;
        }

        foreach (var context in await scheduler.GetCurrentlyExecutingJobs(cancellationToken))
        {
            if (instanceId == context.FireInstanceId)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<bool> StopRunningJob(string instanceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var jobKey = await jobKeyHelper.GetJobKey(instanceId);
            var resultJob = await scheduler.Interrupt(jobKey, cancellationToken);
            return resultJob;
        }
        catch (RestNotFoundException)
        {
            var result = await scheduler.Interrupt(instanceId, cancellationToken);
            return result;
        }
    }

    public async Task<List<PersistanceRunningJobsInfo>> GetPersistanceRunningJobsInfo(CancellationToken cancellationToken = default)
    {
        var result = new List<PersistanceRunningJobsInfo>();
        var runningJobs = await scheduler.GetCurrentlyExecutingJobs(cancellationToken);
        foreach (var context in runningJobs)
        {
            if (context.JobRunTime.TotalSeconds > AppSettings.General.PersistRunningJobsSpan.TotalSeconds)
            {
                if (context.Result is not JobExecutionMetadata metadata)
                {
                    continue;
                }

                var log = metadata.GetLogText();
                var exceptions = metadata.GetExceptionsText();

                if (string.IsNullOrWhiteSpace(log) && string.IsNullOrWhiteSpace(exceptions)) { break; }

                var item = new PersistanceRunningJobsInfo
                {
                    Group = context.JobDetail.Key.Group,
                    Name = context.JobDetail.Key.Name,
                    InstanceId = context.FireInstanceId,
                    Log = log,
                    Exceptions = exceptions,
                    Duration = Convert.ToInt32(context.JobRunTime.TotalMilliseconds)
                };

                result.Add(item);
            }
        }

        return result;
    }

    public static JobBasicDetails MapJobRowDetails(IJobDetail source)
    {
        var result = new JobBasicDetails();
        MapJobRowDetails(source, result);
        return result;
    }

    public static void MapJobRowDetails(IJobDetail source, JobBasicDetails target)
    {
        target.Id = JobKeyHelper.GetJobId(source) ?? string.Empty;
        target.Name = source.Key.Name;
        target.Group = source.Key.Group;
        target.Description = source.Description;
        target.JobType = GetJobTypeName(source);
    }

    public static string GetJobTypeName(IJobDetail source)
    {
        const string system = "SystemJob";
        if (JobKeyHelper.IsSystemJobKey(source.Key))
        {
            return system;
        }

        return GetJobTypeName(source.JobType);
    }

    public static string GetJobTypeName(Type type)
    {
        const string defaultTypeName = "Unknown";
        var jobType = GetJobType(type);
        if (jobType == null) { return defaultTypeName; }

        return jobType.Name;
    }

    private static Type? GetJobType(Type type)
    {
        if (type == null) { return null; }

        var list = new List<Type>();
        Type? localType = type;
        while (localType != null)
        {
            list.Add(localType);
            localType = localType.BaseType;
        }

        var index = list.FindIndex(l => l.Name.StartsWith("Base"));
        if (index <= 0) { index = 0; }
        else { index--; }

        return list[index];
    }

    private static void MapJobExecutionContext(IJobExecutionContext source, RunningJobDetails target)
    {
        target.FireInstanceId = source.FireInstanceId;
        target.NextFireTime = source.NextFireTimeUtc.HasValue ? source.NextFireTimeUtc.Value.DateTime : null;
        target.PreviousFireTime = source.PreviousFireTimeUtc.HasValue ? source.PreviousFireTimeUtc.Value.DateTime : null;
        target.ScheduledFireTime = source.ScheduledFireTimeUtc.HasValue ? source.ScheduledFireTimeUtc.Value.DateTime : null;
        target.FireTime = source.FireTimeUtc.DateTime;
        target.RunTime = source.JobRunTime;
        target.RefireCount = source.RefireCount;
        target.TriggerGroup = source.Trigger.Key.Group;
        target.TriggerName = source.Trigger.Key.Name;
        target.DataMap = Global.ConvertDataMapToDictionary(source.MergedJobDataMap);
        target.TriggerId = TriggerHelper.GetTriggerId(source.Trigger) ?? Consts.Undefined;

        if (target.TriggerGroup == Consts.RecoveringJobsGroup)
        {
            target.TriggerId = Consts.RecoveringJobsGroup;
        }

        if (source.Result is JobExecutionMetadata metadata)
        {
            target.EffectedRows = metadata.EffectedRows;
            target.Progress = metadata.Progress;
            target.ExceptionsCount = metadata.Exceptions.Count();
        }
    }
}