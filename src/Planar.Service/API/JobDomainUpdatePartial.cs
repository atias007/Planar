using Planar.API.Common.Entities;
using Planar.Service.API.Helpers;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Model;
using Quartz;
using System.Linq;
using System.Threading.Tasks;
using static Quartz.Logging.OperationName;

namespace Planar.Service.API
{
    public partial class JobDomain
    {
        public async Task<JobIdResponse> UpdateByFolder(UpdateJobFolderRequest request)
        {
            await ValidateAddFolder(request);
            var yml = await GetJobFileContent(request);
            var dynamicRequest = GetJobDynamicRequest(yml, request.Folder);
            var response = await Add(dynamicRequest);
            return response;
        }

        private static void ValidateUpdateJobOptions(UpdateJobOptions options)
        {
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
            metadata.OldJobProperties = await DataLayer.GetJobProperty(metadata.JobId);
        }

        private async Task<JobIdResponse> Update(AddJobDynamicRequest request, UpdateJobOptions options)
        {
            var metadata = new JobUpdateMetadata();

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

            // Sync Triggers Data
            // TODO: SyncTriggersData(...);

            // ScheduleJob
            await Scheduler.ScheduleJob(metadata.JobDetails, metadata.Triggers, true);

            // Update Properties
            await UpdateJobProperties(request, options, metadata);

            // Return Id
            return new JobIdResponse { Id = metadata.JobId };
        }

        private static async Task UpdateTriggersData(AddJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
        {
            if (options.UpdateTriggersData)
            {
            }
            else
            {
            }
        }

        private static async Task UpdateJobData(AddJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
        {
            if (options.UpdateJobData)
            {
                BuildJobData(request, metadata.JobDetails);
            }
            else
            {
                foreach (var item in metadata.OldJobDetails.JobDataMap)
                {
                    metadata.JobDetails.JobDataMap[item.Key] = item.Value;
                }
            }

            metadata.JobDetails.JobDataMap.Add(Consts.JobId, metadata.JobId);

            await Task.CompletedTask;
        }

        private async Task UpdateJobDetails(AddJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
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

        private async Task UpdateJobProperties(AddJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
        {
            if (options.UpdateProperties)
            {
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
        }

        private async Task UpdateTriggers(AddJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
        {
            var triggers = await Scheduler.GetTriggersOfJob(metadata.JobKey);
            foreach (var item in triggers)
            {
                await Scheduler.UnscheduleJob(item.Key);
            }

            if (options.UpdateTriggers)
            {
                metadata.Triggers = BuildTriggers(request);
            }
            else
            {
                metadata.Triggers = metadata.OldTriggers;
            }
        }

        private async Task ValidateJobPaused(JobKey jobKey)
        {
            var triggers = await Scheduler.GetTriggersOfJob(jobKey);
            var notPaused = triggers
                .Where(t => Scheduler.GetTriggerState(t.Key).Result != TriggerState.Paused)
                .Select(t => $"{t.Key.Group}.{t.Key.Name}")
                .ToList();

            if (notPaused.Any())
            {
                var message = string.Join(',', notPaused);
                throw new RestValidationException("triggers", "the following job triggers are not in pause state. stop the job before make any update");
            }
        }

        private async Task ValidateUpdateJob(AddJobDynamicRequest request, UpdateJobOptions options, JobUpdateMetadata metadata)
        {
            ValidateRequestNoNull(request);
            ValidateUpdateJobOptions(options);
            await ValidateRequestProperties(request);
            metadata.JobKey = await ValidateJobMetadata(request);
            await JobKeyHelper.ValidateJobExists(metadata.JobKey);
            ValidateSystemJob(metadata.JobKey);
            metadata.JobId = await JobKeyHelper.GetJobId(metadata.JobKey);
            await ValidateJobPaused(metadata.JobKey);
            await ValidateJobNotRunning(metadata.JobKey);
        }
    }
}