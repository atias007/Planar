using CommonJob;
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
        private readonly IScheduler _scheduler;

        public TriggerKeyHelper(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public async Task<TriggerKey> GetTriggerKey(JobOrTriggerKey key)
        {
            TriggerKey result;
            if (key.Id.Contains('.'))
            {
                result = GetTriggerKeyByKey(key.Id);
            }
            else
            {
                result = await GetTriggerKeyById(key.Id);
                result ??= GetTriggerKeyByKey(key.Id);
            }

            if (result == null)
            {
                throw new RestNotFoundException($"trigger with id {key.Id} does not exist");
            }

            return result;
        }

        public async Task<TriggerKey> GetTriggerKey(string id)
        {
            return await GetTriggerKey(new JobOrTriggerKey { Id = id });
        }

        public async Task<TriggerKey> GetTriggerKeyById(string triggerId)
        {
            TriggerKey result = null;
            var keys = await _scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
            foreach (var k in keys)
            {
                var triggerDetails = await _scheduler.GetTrigger(k);
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
                throw new PlanarJobException("trigger is null at TriggerKeyHelper.GetTriggerId(ITrigger)");
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

        public static bool Equals(TriggerKey key1, TriggerKey key2)
        {
            if (key1 == null && key2 == null) { return true; }
            if (key1 == null || key2 == null) { return false; }
            return key1.Group.Equals(key2.Group) && key1.Name.Equals(key2);
        }

        public async Task<ITrigger> ValidateTriggerExists(TriggerKey triggerKey)
        {
            var exists = await _scheduler.GetTrigger(triggerKey);

            if (exists == null)
            {
                throw new RestNotFoundException($"trigger with key {triggerKey.Group}.{triggerKey.Name} does not exist");
            }

            return exists;
        }

        public static bool IsSystemTriggerKey(TriggerKey triggerKey)
        {
            return triggerKey.Group == Consts.PlanarSystemGroup;
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