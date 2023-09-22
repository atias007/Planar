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

namespace Planar.Service.General
{
    public class SchedulerUtil
    {
        private readonly IScheduler _scheduler;
        private readonly IServiceProvider _serviceProvider;

        public SchedulerUtil(IServiceProvider provider)
        {
            _serviceProvider = provider;
            _scheduler = _serviceProvider.GetRequiredService<IScheduler>();
        }

        public IScheduler Scheduler => _scheduler;

        internal string SchedulerInstanceId
        {
            get
            {
                return _scheduler.SchedulerInstanceId;
            }
        }

        public async Task Start(CancellationToken cancellationToken = default)
        {
            await _scheduler.Start(cancellationToken);
        }

        public async Task Shutdown(CancellationToken cancellationToken = default)
        {
            await _scheduler.Shutdown(true, cancellationToken);
        }

        public async Task Stop(CancellationToken cancellationToken = default)
        {
            await _scheduler.Standby(cancellationToken);
        }

        public void HealthCheck(ILogger? logger = null)
        {
            if (!IsSchedulerRunning)
            {
                logger?.LogError("HealthCheck fail. IsShutdown={IsShutdown}, InStandbyMode={InStandbyMode}, IsStarted={IsStarted}",
                    _scheduler.IsShutdown, _scheduler.InStandbyMode, _scheduler.IsStarted);
                throw new PlanarException("scheduler is not running");
            }
        }

        public bool IsSchedulerRunning
        {
            get
            {
                return !_scheduler.IsShutdown && !_scheduler.InStandbyMode && _scheduler.IsStarted;
            }
        }

        public async Task<bool> IsJobRunning(JobKey jobKey, CancellationToken cancellationToken = default)
        {
            var allRunning = await _scheduler.GetCurrentlyExecutingJobs(cancellationToken);
            var result = allRunning.AsQueryable().Any(c => KeyHelper.Equals(c.JobDetail.Key, jobKey));
            return result;
        }

        public async Task<RunningJobDetails?> GetRunningJob(string instanceId, CancellationToken cancellationToken = default)
        {
            var jobs = await _scheduler.GetCurrentlyExecutingJobs(cancellationToken);
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
            var jobs = await _scheduler.GetCurrentlyExecutingJobs(cancellationToken);

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

        public async Task<GetRunningDataResponse?> GetRunningData(string instanceId, CancellationToken cancellationToken = default)
        {
            var context = (await _scheduler.GetCurrentlyExecutingJobs(cancellationToken))
                .FirstOrDefault(j => j.FireInstanceId == instanceId);

            if (context == null) { return null; }

            var log = string.Empty;
            var exceptions = string.Empty;
            var count = 0;

            if (context.Result is JobExecutionMetadata metadata)
            {
                log = metadata.GetLog();
                exceptions = metadata.GetExceptionsText();
                count = metadata.Exceptions.Count;
            }

            var response = new GetRunningDataResponse
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

            foreach (var context in await _scheduler.GetCurrentlyExecutingJobs(cancellationToken))
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
                var helper = _serviceProvider.GetRequiredService<JobKeyHelper>();
                var jobKey = await helper.GetJobKey(instanceId);
                var resultJob = await _scheduler.Interrupt(jobKey, cancellationToken);
                return resultJob;
            }
            catch (RestNotFoundException)
            {
                var result = await _scheduler.Interrupt(instanceId, cancellationToken);
                return result;
            }
        }

        public async Task<List<PersistanceRunningJobsInfo>> GetPersistanceRunningJobsInfo(CancellationToken cancellationToken = default)
        {
            var result = new List<PersistanceRunningJobsInfo>();
            var runningJobs = await _scheduler.GetCurrentlyExecutingJobs(cancellationToken);
            foreach (var context in runningJobs)
            {
                if (context.JobRunTime.TotalSeconds > AppSettings.General.PersistRunningJobsSpan.TotalSeconds)
                {
                    if (context.Result is not JobExecutionMetadata metadata)
                    {
                        continue;
                    }

                    var log = metadata.GetLog();
                    var exceptions = metadata.GetExceptionsText();

                    if (string.IsNullOrEmpty(log) && string.IsNullOrEmpty(exceptions)) { break; }

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

        public static JobRowDetails MapJobRowDetails(IJobDetail source)
        {
            var result = new JobRowDetails();
            MapJobRowDetails(source, result);
            return result;
        }

        public static void MapJobRowDetails(IJobDetail source, JobRowDetails target)
        {
            target.Id = JobKeyHelper.GetJobId(source) ?? string.Empty;
            target.Name = source.Key.Name;
            target.Group = source.Key.Group;
            target.Description = source.Description;
            target.JobType = GetJobTypeName(source.JobType);
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
                target.ExceptionsCount = metadata.Exceptions.Count;
            }
        }
    }
}