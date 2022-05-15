using Planar.API.Common.Entities;
using Planar.Service.Exceptions;
using Planar.Service.Model.Metadata;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Threading.Tasks;

namespace Planar.Service.API.Helpers
{
    public static class JobKeyHelper
    {
        private static IScheduler Scheduler
        {
            get
            {
                return MainService.Scheduler;
            }
        }

        public static async Task<JobKey> GetJobKey(string id)
        {
            return await GetJobKey(new JobOrTriggerKey { Id = id });
        }

        public static async Task<JobKey> GetJobKey(JobOrTriggerKey key)
        {
            JobKey result;
            if (key.Id.Contains('.'))
            {
                result = GetJobKeyByKey(key.Id);
            }
            else
            {
                result = await GetJobKeyById(key.Id);
                if (result == null)
                {
                    result = GetJobKeyByKey(key.Id);
                }
            }

            await ValidateJobExists(result);

            return result;
        }

        public static string GetJobId(IJobDetail job)
        {
            if (job == null)
            {
                throw new NullReferenceException("job is null at JobKeyHelper.GetJobId(IJobDetail)");
            }

            if (job.JobDataMap.TryGetValue(Consts.JobId, out var id))
            {
                return Convert.ToString(id);
            }

            return null;
        }

        public static string GetJobId(IJobExecutionContext context)
        {
            return GetJobId(context.JobDetail);
        }

        private static async Task ValidateJobExists(JobKey jobKey)
        {
            var exists = await Scheduler.GetJobDetail(jobKey);

            if (exists == null)
            {
                throw new NotExistsException($"job with id {jobKey.Name} or key {jobKey.Group}.{jobKey.Name} not exists");
            }
        }

        public static JobKey GetJobKey(JobMetadata metadata)
        {
            return string.IsNullOrEmpty(metadata.Group) ?
                            new JobKey(metadata.Name) :
                            new JobKey(metadata.Name, metadata.Group);
        }

        private static async Task<JobKey> GetJobKeyById(string jobId)
        {
            JobKey result = null;
            var keys = await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            foreach (var k in keys)
            {
                var jobDetails = await Scheduler.GetJobDetail(k);
                if (jobDetails != null)
                {
                    var id = GetJobId(jobDetails);
                    if (id == jobId)
                    {
                        result = k;
                        break;
                    }
                }
            }

            return result;
        }

        private static JobKey GetJobKeyByKey(string key)
        {
            JobKey result = null;
            if (key != null)
            {
                var index = key.IndexOf(".");
                if (index == -1)
                {
                    result = new JobKey(key);
                }
                else
                {
                    result = new JobKey(key[(index + 1)..], key[0..index]);
                }
            }

            return result;
        }
    }
}