﻿using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using Planar.Service.Monitor;
using Quartz;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public partial class JobDomain
    {
        public async Task<JobIdResponse> UpdateByPath(UpdateJobPathRequest request)
        {
            await ValidateAddPath(request);
            var yml = await GetJobFileContent(request);
            var dynamicRequest = GetJobDynamicRequest(yml);
            dynamic properties = dynamicRequest.Properties ?? new ExpandoObject();
            properties["path"] = request.Folder;
            var response = await Update(dynamicRequest, request.UpdateJobOptions);
            return response;
        }

        private static T? Clone<T>(T obj)
            where T : class, new()
        {
            var json = YmlUtil.Serialize(obj);
            var result = YmlUtil.Deserialize<T>(json);
            return result;
        }

        private static IEnumerable<BaseTrigger> GetAllTriggers(SetJobRequest request)
        {
            var allTriggers = new List<BaseTrigger>();
            if (request.SimpleTriggers != null)
            {
                allTriggers.AddRange(request.SimpleTriggers);
            }

            if (request.CronTriggers != null)
            {
                allTriggers.AddRange(request.CronTriggers);
            }

            return allTriggers;
        }

        private static DateTimeOffset GetTriggerStartAt(ITrigger trigger)
        {
            var delayStart = DelayStartTriggerDateTime;
            var next = trigger.GetNextFireTimeUtc();
            if (next == null || next < delayStart)
            {
                return delayStart;
            }
            else
            {
                return next.Value;
            }
        }

        private static IReadOnlyCollection<ITrigger> RebuildOldTriggers(IReadOnlyCollection<ITrigger> triggers)
        {
            var result = new List<ITrigger>();
            foreach (var item in triggers)
            {
                var builder = item.GetTriggerBuilder();
                var start = GetTriggerStartAt(item);
                builder.StartAt(start);

                var trigger = builder.Build();
                result.Add(trigger);
            }

            return new ReadOnlyCollection<ITrigger>(result);
        }

        private static void SyncJobData(SetJobRequest request, JobUpdateMetadata metadata)
        {
            foreach (var item in metadata.OldJobDetails.JobDataMap)
            {
                request.JobData.Put<string?>(item.Key, PlanarConvert.ToString(item.Value));
            }
        }

        private static void SyncTriggersData(SetJobRequest request, JobUpdateMetadata metadata)
        {
            var allTriggers = GetAllTriggers(request);
            foreach (var oldTrigger in metadata.OldTriggers)
            {
                var updateTrigger = allTriggers.FirstOrDefault(t => t.Group == oldTrigger.Key.Group && t.Name == oldTrigger.Key.Name);
                if (updateTrigger == null) { continue; }
                foreach (var data in oldTrigger.JobDataMap)
                {
                    updateTrigger.TriggerData.Put<string?>(data.Key, PlanarConvert.ToString(data.Value));
                }
            }
        }

        private static async Task UpdateJobAuthor(SetJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
        {
            if (options.UpdateJobDetails)
            {
                metadata.JobDetails.JobDataMap.Put(Consts.Author, request.Author);
                if (request.LogRetentionDays.HasValue)
                {
                    metadata.JobDetails.JobDataMap.Put(Consts.LogRetentionDays, request.LogRetentionDays.Value.ToString());
                }
            }
            else
            {
                metadata.JobDetails.JobDataMap.Put(Consts.Author, metadata.Author);
                if (metadata.LogRetentionDays.HasValue)
                {
                    metadata.JobDetails.JobDataMap.Put(Consts.LogRetentionDays, metadata.LogRetentionDays.Value.ToString());
                }
            }

            await Task.CompletedTask;
        }

        private static async Task UpdateJobData(SetJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
        {
            if (options.UpdateJobData)
            {
                // Preserve the same id
                request.JobData.Add(Consts.JobId, metadata.JobId);
            }
            else
            {
                // Sync the old data including the id
                request.JobData.Clear();
                SyncJobData(request, metadata);
            }

            BuildJobData(request, metadata.JobDetails);

            await Task.CompletedTask;
        }

        private static void ValidateUpdateJobOptions(UpdateJobOptions options)
        {
            if (options == null)
            {
                throw new RestValidationException("request", "options property is null or empty");
            }

            if (options.IsEmpty)
            {
                throw new RestValidationException("request", "all request options are false. no update will occur");
            }
        }

        private async Task FillRollbackData(JobUpdateMetadata metadata)
        {
            metadata.OldJobDetails =
                await Scheduler.GetJobDetail(metadata.JobKey)
                ?? throw new RestGeneralException($"job with key '{KeyHelper.GetKeyTitle(metadata.JobKey)}' could not be found");

            metadata.OldTriggers = await Scheduler.GetTriggersOfJob(metadata.JobKey);
            await Scheduler.DeleteJob(metadata.JobKey);
            metadata.EnableRollback();
            metadata.OldJobProperties = await DataLayer.GetJobProperty(metadata.JobId);
            metadata.Author = JobHelper.GetJobAuthor(metadata.OldJobDetails);
            metadata.LogRetentionDays = JobHelper.GetLogRetentionDays(metadata.OldJobDetails);
        }

        private async Task RollBack(JobUpdateMetadata metadata)
        {
            if (metadata == null) { return; }
            if (metadata.OldJobDetails == null) { return; }
            if (!metadata.RollbackEnabled) { return; }

            var property = new JobProperty { JobId = metadata.JobId, Properties = metadata.OldJobProperties };
            await Scheduler.ScheduleJob(metadata.OldJobDetails, metadata.OldTriggers, true);
            await Scheduler.PauseJob(metadata.JobKey);
            await Resolve<JobData>().UpdateJobProperty(property);
        }

        private async Task<JobIdResponse> Update(SetJobDynamicRequest request, UpdateJobOptions options)
        {
            var metadata = new JobUpdateMetadata();

            try
            {
                return await UpdateInner(request, options, metadata);
            }
            catch
            {
                await RollBack(metadata);
                throw;
            }
        }

        private async Task<JobIdResponse> UpdateInner(SetJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
        {
            var monitor = Resolve<MonitorUtil>();
            monitor.LockJobEvent(metadata.JobKey, MonitorEvents.JobDeleted, MonitorEvents.JobAdded, MonitorEvents.JobPaused);

            // Validation
            await ValidateUpdateJob(request, options, metadata);

            // Save for rollback
            await FillRollbackData(metadata);

            // Clone request for audit
            var cloneRequest = Clone(request);

            // Update Job Details (JobType+Concurrent, JobGroup, JobName, Description, Durable)
            await UpdateJobDetails(request, options, metadata);

            // Sync Job Data
            await UpdateJobData(request, options, metadata);

            // Update the author
            await UpdateJobAuthor(request, options, metadata);

            // Update Triggers
            await UpdateTriggers(request, options, metadata);

            // ScheduleJob
            await Scheduler.ScheduleJob(metadata.JobDetails, metadata.Triggers, true);
            await Scheduler.PauseJob(metadata.JobKey);

            // Update Properties
            await UpdateJobProperties(request, options, metadata);

            // Audit
            AuditJobSafe(metadata.JobKey, "job updated", new { request = cloneRequest, options });

            // Return Id
            return new JobIdResponse { Id = metadata.JobId };
        }

        private async Task UpdateJobDetails(SetJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
        {
            if (options.UpdateJobDetails)
            {
                await Scheduler.DeleteJob(metadata.JobKey);
                metadata.JobDetails = BuildJobDetails(request, metadata.JobKey);
            }
            else
            {
                metadata.JobDetails = CloneJobDetails(metadata.OldJobDetails);
            }
        }

        private async Task UpdateJobProperties(SetJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
        {
            if (!options.UpdateProperties) { return; }

            var jobPropertiesYml = GetJopPropertiesYml(request);
            var property = new JobProperty { JobId = metadata.JobId, Properties = jobPropertiesYml };
            if (string.IsNullOrEmpty(metadata.OldJobProperties))
            {
                await DataLayer.AddJobProperty(property);
            }
            else
            {
                await DataLayer.UpdateJobProperty(property);
            }
        }

        private async Task UpdateTriggers(SetJobRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
        {
            foreach (var item in metadata.OldTriggers)
            {
                await Scheduler.UnscheduleJob(item.Key);
            }

            if (options.UpdateTriggers)
            {
                SyncTriggersData(request, metadata);
                metadata.Triggers = BuildTriggers(request, metadata.JobId);
            }
            else
            {
                var triggers = RebuildOldTriggers(metadata.OldTriggers);
                metadata.Triggers = triggers;
            }
        }

        private async Task ValidateUpdateJob(SetJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
        {
            ValidateRequestNoNull(request);
            ValidateUpdateJobOptions(options);
            await ValidateRequestProperties(request);
            metadata.JobKey = ValidateJobMetadata(request, Scheduler);
            await JobKeyHelper.ValidateJobExists(metadata.JobKey);
            ValidateSystemJob(metadata.JobKey);
            metadata.JobId =
                await JobKeyHelper.GetJobId(metadata.JobKey) ??
                throw new RestGeneralException($"could not find job id for job key '{KeyHelper.GetKeyTitle(metadata.JobKey)}'");
            await ValidateJobPaused(metadata.JobKey);
            await ValidateJobNotRunning(metadata.JobKey);
        }
    }
}