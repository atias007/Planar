using Planar.API.Common.Entities;
using Planar.Service.Exceptions;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Threading.Tasks;

namespace Planar.Service.API.Helpers
{
    public class TriggerKeyHelper
    {
        private static IScheduler Scheduler
        {
            get
            {
                return MainService.Scheduler;
            }
        }

        public static async Task<TriggerKey> GetTriggerKey(JobOrTriggerKey key)
        {
            TriggerKey result;
            if (key.Id.Contains('.'))
            {
                result = GetTriggerKeyByKey(key.Id);
            }
            else
            {
                result = await GetTriggerKeyById(key.Id);
                if (result == null)
                {
                    result = GetTriggerKeyByKey(key.Id);
                }
            }

            if (result == null)
            {
                throw new NotExistsException($"trigger with id {key.Id} not exists");
            }

            return result;
        }

        private static async Task<TriggerKey> GetTriggerKeyById(string triggerId)
        {
            TriggerKey result = null;
            var keys = await Scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
            foreach (var k in keys)
            {
                var triggerDetails = await Scheduler.GetTrigger(k);
                if (triggerDetails != null && triggerDetails.JobDataMap.TryGetValue(Consts.TriggerId, out var id))
                {
                    if (Convert.ToString(id) == triggerId)
                    {
                        result = k;
                        break;
                    }
                }
            }

            return result;
        }

        private static TriggerKey GetTriggerKeyByKey(string key)
        {
            TriggerKey result = null;
            if (key != null)
            {
                var index = key.IndexOf(".");
                if (index == -1)
                {
                    result = new TriggerKey(key);
                }
                else
                {
                    result = new TriggerKey(key[(index + 1)..], key[0..index]);
                }
            }

            return result;
        }

        private static async Task ValidateTriggerExists(TriggerKey triggerKey)
        {
            var exists = await Scheduler.GetTrigger(triggerKey);

            if (exists == null)
            {
                throw new NotExistsException($"trigger with name: {triggerKey.Name} and group: {triggerKey.Group} not exists");
            }
        }
    }
}