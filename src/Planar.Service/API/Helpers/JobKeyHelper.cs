using CommonJob;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.API.Helpers;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Linq;
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

        public static bool Compare(JobKey jobKeyA, JobKey jobKeyB)
        {
            return jobKeyA.Name == jobKeyB.Name && jobKeyA.Group == jobKeyB.Group;
        }

        public static string GetJobId(IJobDetail job)
        {
            return JobIdHelper.GetJobId(job);
        }

        public static JobKey GetJobKey(SetJobRequest metadata)
        {
            return string.IsNullOrEmpty(metadata.Group) ?
                            new JobKey(metadata.Name) :
                            new JobKey(metadata.Name, metadata.Group);
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

        public async Task<string> GetJobId(MonitorAction action)
        {
            var key = $"{action.JobGroup}.{action.JobName}";
            return await GetJobId(key);
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

        public async Task<bool> IsJobGroupExists(string group)
        {
            var all = await _scheduler.GetJobGroupNames();
            return all.Contains(group);
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

        public static bool IsSystemJobKey(JobKey jobKey)
        {
            return jobKey.Group == Consts.PlanarSystemGroup;
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