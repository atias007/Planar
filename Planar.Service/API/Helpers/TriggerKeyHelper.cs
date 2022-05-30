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
                result = await GetTriggerKey(key.Id);
                if (result == null)
                {
                    result = GetTriggerKeyByKey(key.Id);
                }
            }

            if (result == null)
            {
                throw new RestNotFoundException($"trigger with id {key.Id} not exists");
            }

            return result;
        }

        public static async Task<TriggerKey> GetTriggerKey(string triggerId)
        {
            TriggerKey result = null;
            var keys = await Scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
            foreach (var k in keys)
            {
                var triggerDetails = await Scheduler.GetTrigger(k);
                var id = GetTriggerId(triggerDetails);
                if (id == triggerId)
                {
                    result = k;
                    break;
                }
            }

            return result;
        }

        public static string GetTriggerId(ITrigger trigger)
        {
            if (trigger == null)
            {
                throw new NullReferenceException("trigger is null at TriggerKeyHelper.GetTriggerId(ITrigger)");
            }

            if (trigger.JobDataMap.TryGetValue(Consts.TriggerId, out var id))
            {
                return Convert.ToString(id);
            }

            return null;
        }

        public static string GetTriggerId(IJobExecutionContext context)
        {
            return GetTriggerId(context.Trigger);
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
    }
}