using CommonJob;
using Planar.API.Common.Entities;
using Planar.Service.Exceptions;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Threading.Tasks;

namespace Planar.Service.API.Helpers
{
    public class JobKeyHelper
    {
        private readonly IScheduler _scheduler;

        public JobKeyHelper(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public async Task<JobKey> GetJobKey(string id)
        {
            return await GetJobKey(new JobOrTriggerKey { Id = id });
        }

        public async Task<JobKey> GetJobKey(JobOrTriggerKey key)
        {
            JobKey result;
            if (key.Id.Contains('.'))
            {
                result = GetJobKeyByKey(key.Id);
            }
            else
            {
                result = await GetJobKeyById(key.Id);
                result ??= GetJobKeyByKey(key.Id);
            }

            await ValidateJobExists(result);

            return result;
        }

        public static string GetJobId(IJobDetail job)
        {
            if (job == null)
            {
                throw new PlanarJobException("job is null at JobKeyHelper.GetJobId(IJobDetail)");
            }

            if (job.JobDataMap.TryGetValue(Consts.JobId, out var id))
            {
                return Convert.ToString(id);
            }

            return null;
        }

        public async Task<string> GetJobId(JobKey jobKey)
        {
            var job = await ValidateJobExists(jobKey);
            return GetJobId(job);
        }

        public async Task<string> GetJobId(string id)
        {
            var jobKey = await GetJobKey(id);
            var jobId = await GetJobId(jobKey);
            return jobId;
        }

        public async Task<IJobDetail> ValidateJobExists(JobKey jobKey)
        {
            var exists = await _scheduler.GetJobDetail(jobKey);

            if (exists == null)
            {
                throw new RestNotFoundException($"job with key {jobKey.Group}.{jobKey.Name} does not exist");
            }

            return exists;
        }

        public static JobKey GetJobKey(SetJobRequest metadata)
        {
            return string.IsNullOrEmpty(metadata.Group) ?
                            new JobKey(metadata.Name) :
                            new JobKey(metadata.Name, metadata.Group);
        }

        public async Task<JobKey> GetJobKeyById(string jobId)
        {
            JobKey result = null;
            var keys = await _scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            foreach (var k in keys)
            {
                var jobDetails = await _scheduler.GetJobDetail(k);
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

        public static bool Compare(JobKey jobKeyA, JobKey jobKeyB)
        {
            return jobKeyA.Name == jobKeyB.Name && jobKeyA.Group == jobKeyB.Group;
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