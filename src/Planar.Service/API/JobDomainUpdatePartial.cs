using Azure.Core;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public partial class JobDomain
    {
        public async Task<JobIdResponse> UpdateByFolder(UpdateJobFolderRequest request)
        {
            await ValidateAddFolder(request);
            var yml = await GetJobFileContent(request);
            var dynamicRequest = GetJobDynamicRequest(yml, request.Folder);
            var response = await Update(dynamicRequest, request.UpdateJobOptions);
            return response;
        }

        public async Task<JobIdResponse> Update<TProperties>(UpdateJobRequest<TProperties> genericRequest)
           where TProperties : class, new()
        {
            ValidateRequestNoNull(genericRequest);
            var dynamicRequest = Mapper.Map<SetJobRequest<TProperties>, SetJobDynamicRequest>(genericRequest);
            var response = await Update(dynamicRequest, genericRequest.UpdateJobOptions);
            return response;
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

        private static void SyncJobData(SetJobRequest request, JobUpdateMetadata metadata)
        {
            foreach (var item in metadata.OldJobDetails.JobDataMap)
            {
                request.JobData.Upsert(item.Key, Convert.ToString(item.Value));
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
                    updateTrigger.TriggerData.Upsert(data.Key, Convert.ToString(data.Value));
                }
            }
        }

        private static async Task UpdateJobData(SetJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
        {
            if (options.UpdateJobData)
            {
                request.JobData.Add(Consts.JobId, metadata.JobId);
            }
            else
            {
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
            metadata.OldJobDetails = await Scheduler.GetJobDetail(metadata.JobKey);
            metadata.OldTriggers = await Scheduler.GetTriggersOfJob(metadata.JobKey);
            await Scheduler.DeleteJob(metadata.JobKey);
            metadata.EnableRollback();
            metadata.OldJobProperties = await DataLayer.GetJobProperty(metadata.JobId);
        }

        private async Task<JobIdResponse> Update(SetJobDynamicRequest request, UpdateJobOptions options)
        {
            var metadata = new JobUpdateMetadata();

            using var transaction = GetTransaction();
            var result = await UpdateInner(request, options, metadata);
            transaction.Complete();
            return result;

            // TODO: check for rollback
            ////try
            ////{
            ////    return await UpdateInner(request, options, metadata);
            ////}
            ////catch
            ////{
            ////    await RollBack(metadata);
            ////    throw;
            ////}
        }

        private async Task RollBack(JobUpdateMetadata metadata)
        {
            if (metadata == null) { return; }
            if (metadata.OldJobDetails == null) { return; }
            if (!metadata.RollbackEnabled) { return; }

            var property = new JobProperty { JobId = metadata.JobId, Properties = metadata.OldJobProperties };
            await Scheduler.ScheduleJob(metadata.OldJobDetails, metadata.OldTriggers, true);
            await Scheduler.PauseJob(metadata.JobKey);
            await DataLayer.UpdateJobProperty(property);
        }

        private async Task<JobIdResponse> UpdateInner(SetJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
        {
            // Validation
            await ValidateUpdateJob(request, options, metadata);

            // Save for rollback
            await FillRollbackData(metadata);

            // Update Job Details
            await UpdateJobDetails(request, options, metadata);

            // Sync Job Data
            await UpdateJobData(request, options, metadata);

            // Update Triggers
            await UpdateTriggers(request, options, metadata);

            // ScheduleJob
            await Scheduler.ScheduleJob(metadata.JobDetails, metadata.Triggers, true);
            await Scheduler.PauseJob(metadata.JobKey);

            // Update Properties
            await UpdateJobProperties(request, options, metadata);

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
                metadata.JobDetails = metadata.OldJobDetails;
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
                metadata.Triggers = BuildTriggers(request);
            }
            else
            {
                metadata.Triggers = metadata.OldTriggers;
            }
        }

        private async Task ValidateUpdateJob(SetJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
        {
            ValidateRequestNoNull(request);
            ValidateUpdateJobOptions(options);
            await ValidateRequestProperties(request);
            metadata.JobKey = ValidateJobMetadata(request);
            await JobKeyHelper.ValidateJobExists(metadata.JobKey);
            ValidateSystemJob(metadata.JobKey);
            metadata.JobId = await JobKeyHelper.GetJobId(metadata.JobKey);
            await ValidateJobPaused(metadata.JobKey);
            await ValidateJobNotRunning(metadata.JobKey);
        }
    }
}