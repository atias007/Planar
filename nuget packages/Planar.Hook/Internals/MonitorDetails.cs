using System;
using System.Collections.Generic;

namespace Planar.Hook
{
    internal class MonitorDetails : Monitor, IMonitorDetails
    {
#if NETSTANDARD2_0
        public string JobDescription { get; set; }
        public string TriggerName { get; set; }
        public string TriggerGroup { get; set; }
        public string Calendar { get; set; }
        public string Author { get; set; }
        public IReadOnlyDictionary<string, string> MergedJobDataMap { get; set; } = new Dictionary<string, string>();

#else
        public string? JobDescription { get; set; }
        public string? TriggerName { get; set; }
        public string? TriggerGroup { get; set; }
        public string? Calendar { get; set; }
        public string? Author { get; set; }
        public IReadOnlyDictionary<string, string?> MergedJobDataMap { get; set; } = new Dictionary<string, string?>();

#endif
        public string JobName { get; set; } = string.Empty;
        public string JobGroup { get; set; } = string.Empty;
        public string JobId { get; set; } = string.Empty;
        public bool Durable { get; set; }
        public string TriggerId { get; set; } = string.Empty;
        public string FireInstanceId { get; set; } = string.Empty;
        public DateTime FireTime { get; set; }
        public TimeSpan JobRunTime { get; set; }
        public bool Recovering { get; set; }

#if NETSTANDARD2_0

        internal void AddMergedJobDataMap(string key, string value)
        {
            var mergedJobDataMap = (Dictionary<string, string>)MergedJobDataMap;
            mergedJobDataMap.Add(key, value);
        }

#else
        internal void AddMergedJobDataMap(string key, string? value)
        {
            var mergedJobDataMap = (Dictionary<string, string?>)MergedJobDataMap;
            mergedJobDataMap.Add(key, value);
        }
#endif
    }
}