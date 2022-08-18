using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
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
    public static class SchedulerUtil
    {
        public static async Task Start(CancellationToken cancellationToken = default)
        {
            await MainService.Scheduler.Start(cancellationToken);
        }

        public static async Task Stop(CancellationToken cancellationToken = default)
        {
            await MainService.Scheduler.Standby(cancellationToken);
        }

        public static void HealthCheck(ILogger logger = null)
        {
            if (!IsSchedulerRunning)
            {
                logger?.LogError("HealthCheck fail. IsShutdown={IsShutdown}, InStandbyMode={InStandbyMode}, IsStarted={IsStarted}",
                    MainService.Scheduler.IsShutdown, MainService.Scheduler.InStandbyMode, MainService.Scheduler.IsStarted);
                throw new PlanarException("Scheduler is not running");
            }
        }

        public static bool IsSchedulerRunning
        {
            get
            {
                return !MainService.Scheduler.IsShutdown && !MainService.Scheduler.InStandbyMode && MainService.Scheduler.IsStarted;
            }
        }

        public static async Task<bool> IsJobRunning(JobKey jobKey, CancellationToken cancellationToken = default)
        {
            var allRunning = await MainService.Scheduler.GetCurrentlyExecutingJobs(cancellationToken);
            var result = allRunning.AsQueryable().Any(c => c.JobDetail.Key.Name == jobKey.Name && c.JobDetail.Key.Group == jobKey.Group);
            return result;
        }

        public static async Task<RunningJobDetails> GetRunningJob(string instanceId, CancellationToken cancellationToken = default)
        {
            var jobs = await MainService.Scheduler.GetCurrentlyExecutingJobs(cancellationToken);
            var context = jobs.FirstOrDefault(j => j.FireInstanceId == instanceId);
            if (context == null) { return null; }
            var details = new RunningJobDetails();
            MapJobRowDetails(context.JobDetail, details);
            MapJobExecutionContext(context, details);
            return details;
        }

        public static async Task<List<RunningJobDetails>> GetRunningJobs(CancellationToken cancellationToken = default)
        {
            var result = new List<RunningJobDetails>();
            var jobs = await MainService.Scheduler.GetCurrentlyExecutingJobs(cancellationToken);

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

        public static async Task<GetRunningInfoResponse> GetRunningInfo(string instanceId, CancellationToken cancellationToken = default)
        {
            var context = (await MainService.Scheduler.GetCurrentlyExecutingJobs(cancellationToken))
                .FirstOrDefault(j => j.FireInstanceId == instanceId);

            if (context == null) { return null; }

            var information = string.Empty;
            var exceptions = string.Empty;

            if (context.Result is JobExecutionMetadata metadata)
            {
                information = metadata.GetInformation();
                exceptions = metadata.GetExceptionsText();
            }

            var response = new GetRunningInfoResponse { Information = information, Exceptions = exceptions };
            return response;
        }

        private static async Task<bool> IsRunningInstanceExistOnCluster(string instanceId, CancellationToken cancellationToken = default)
        {
            var result = await IsRunningInstanceExistOnLocal(instanceId, cancellationToken);
            if (result) { return true; }

            if (AppSettings.Clustering)
            {
                var dal = MainService.Resolve<DataLayer>();
                var logger = MainService.Resolve<ILogger<ClusterUtil>>();
                result = await new ClusterUtil(dal, logger).IsRunningInstanceExist(instanceId);
            }

            return result;
        }

        public static async Task<bool> IsRunningInstanceExistOnLocal(string instanceId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                return false;
            }

            foreach (var context in await MainService.Scheduler.GetCurrentlyExecutingJobs(cancellationToken))
            {
                if (instanceId == context.FireInstanceId)
                {
                    return true;
                }
            }

            return false;
        }

        public static async Task<bool> StopRunningJob(string instanceId, CancellationToken cancellationToken = default)
        {
            try
            {
                var jobKey = await JobKeyHelper.GetJobKey(instanceId);
                var resultJob = await MainService.Scheduler.Interrupt(jobKey, cancellationToken);
                return resultJob;
            }
            catch (RestNotFoundException)
            {
                if (!await IsRunningInstanceExistOnCluster(instanceId, cancellationToken))
                {
                    throw new RestNotFoundException($"instance id '{instanceId}' is not running");
                }

                var result = await MainService.Scheduler.Interrupt(instanceId, cancellationToken);
                return result;
            }
        }

        public static async Task<List<PersistanceRunningJobsInfo>> GetPersistanceRunningJobsInfo(CancellationToken cancellationToken = default)
        {
            var result = new List<PersistanceRunningJobsInfo>();
            var runningJobs = await MainService.Scheduler.GetCurrentlyExecutingJobs(cancellationToken);
            foreach (var context in runningJobs)
            {
                if (context.JobRunTime.TotalSeconds > AppSettings.PersistRunningJobsSpan.TotalSeconds)
                {
                    if (context.Result is not JobExecutionMetadata metadata)
                    {
                        continue;
                    }

                    var information = metadata.GetInformation();
                    var exceptions = metadata.GetExceptionsText();

                    if (string.IsNullOrEmpty(information) && string.IsNullOrEmpty(exceptions)) { break; }

                    var item = new PersistanceRunningJobsInfo
                    {
                        Group = context.JobDetail.Key.Group,
                        Name = context.JobDetail.Key.Name,
                        InstanceId = context.FireInstanceId,
                        Information = information,
                        Exceptions = exceptions
                    };

                    result.Add(item);
                }
            }

            return result;
        }

        public static void MapJobRowDetails(IJobDetail source, JobRowDetails target)
        {
            target.Id = JobKeyHelper.GetJobId(source);
            target.Name = source.Key.Name;
            target.Group = source.Key.Group;
            target.Description = source.Description;
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
            target.TriggerId = TriggerKeyHelper.GetTriggerId(source);

            if (target.TriggerGroup == Consts.RecoveringJobsGroup)
            {
                target.TriggerId = Consts.RecoveringJobsGroup;
            }

            if (source.Result is JobExecutionMetadata metadata)
            {
                target.EffectedRows = metadata.EffectedRows;
                target.Progress = metadata.Progress;
            }
        }
    }
}