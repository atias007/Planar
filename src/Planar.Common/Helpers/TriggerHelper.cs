using Polly;
using Quartz;
using System;

namespace Planar.Common.Helpers
{
    public static class TriggerHelper
    {
        public static bool Equals(TriggerKey key1, TriggerKey key2)
        {
            if (key1 == null && key2 == null) { return true; }
            if (key1 == null || key2 == null) { return false; }
            return key1.Group.Equals(key2.Group) && key1.Name.Equals(key2);
        }

        public static int GetMaxRetriesWithDefault(ITrigger? trigger)
        {
            var triggerMaxRetries = GetMaxRetries(trigger);
            var maxRetries = GetMaxRetriesInner(triggerMaxRetries);
            return maxRetries;
        }

        public static TimeSpan GetTimeoutWithDefault(ITrigger? trigger)
        {
            var triggerTimeout = GetTimeout(trigger);
            var timeout = GetTimeoutInner(triggerTimeout);
            return timeout;
        }

        public static int? GetMaxRetries(ITrigger? trigger)
        {
            if (trigger == null) { return null; }
            if (!trigger.JobDataMap.TryGetValue(Consts.MaxRetries, out var objValue)) { return null; }
            var strValue = PlanarConvert.ToString(objValue);
            if (string.IsNullOrEmpty(strValue)) { return null; }
            if (!int.TryParse(strValue, out var intVal)) { return null; }
            return intVal;
        }

        public static TimeSpan? GetTimeout(ITrigger? trigger)
        {
            if (trigger == null) { return null; }
            if (!trigger.JobDataMap.TryGetValue(Consts.TriggerTimeout, out var objTiks)) { return null; }
            var strTicks = PlanarConvert.ToString(objTiks);
            if (string.IsNullOrEmpty(strTicks)) { return null; }
            if (!long.TryParse(strTicks, out var lngTicks)) { return null; }
            var ts = TimeSpan.FromTicks(lngTicks);
            return ts;
        }

        public static string? GetTriggerId(ITrigger? trigger)
        {
            if (trigger == null) { return null; }

            if (!trigger.JobDataMap.TryGetValue(Consts.TriggerId, out var id)) { return null; }

            return PlanarConvert.ToString(id);
        }

        public static bool IsSystemTriggerKey(TriggerKey triggerKey)
        {
            return triggerKey.Group == Consts.PlanarSystemGroup;
        }

        public static bool HasRetry(ITrigger trigger)
        {
            var result = trigger.JobDataMap.Contains(Consts.RetrySpan);
            return result;
        }

        public static int? GetRetryNumber(ITrigger trigger)
        {
            if (!trigger.JobDataMap.TryGetValue(Consts.RetryCounter, out var objCount)) { return null; }
            var strCount = PlanarConvert.ToString(objCount);
            if (string.IsNullOrEmpty(strCount)) { return null; }
            if (!int.TryParse(strCount, out var intCount)) { return null; }
            return intCount;
        }

        public static TimeSpan? GetRetrySpan(ITrigger trigger)
        {
            var value = trigger.JobDataMap[Consts.RetrySpan];
            var spanValue = Convert.ToString(value);
            if (string.IsNullOrEmpty(spanValue)) { return null; }
            if (!TimeSpan.TryParse(spanValue, out TimeSpan span)) { return null; }
            return span;
        }

        public static bool IsRetryTrigger(ITrigger trigger)
        {
            var result = trigger.Key.Name.StartsWith(Consts.RetryTriggerNamePrefix);
            return result;
        }

        public static string GetKeyTitle(ITrigger trigger)
        {
            return trigger.Key.Name;
        }

        private static int GetMaxRetriesInner(int? specificMaxRetries = null)
        {
            if (specificMaxRetries.HasValue && specificMaxRetries != 0)
            {
                return specificMaxRetries.Value;
            }

            return Consts.DefaultMaxRetries;
        }

        private static TimeSpan GetTimeoutInner(TimeSpan? specificTimeout = null)
        {
            if (specificTimeout.HasValue && specificTimeout != TimeSpan.Zero)
            {
                return specificTimeout.Value;
            }

            return AppSettings.JobAutoStopSpan;
        }
    }
}