﻿using Planner.API.Common.Entities;
using Planner.Service.Exceptions;
using Planner.Service.Model.Metadata;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Threading.Tasks;

namespace Planner.Service.API.Helpers
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
                if (jobDetails != null && jobDetails.JobDataMap.TryGetValue(Consts.JobId, out var id))
                {
                    if (Convert.ToString(id) == jobId)
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