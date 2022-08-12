using System;
using System.Collections.Generic;

namespace Planar.Job
{
    internal class TriggerDetail : ITriggerDetail
    {
        public IKey Key { get; set; }
        public string Description { get; set; }
        public string CalendarName { get; set; }
        public SortedDictionary<string, string> TriggerDataMap { get; set; }
        public DateTimeOffset? FinalFireTime { get; set; }
        public int MisfireInstruction { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public int Priority { get; set; }
        public bool HasMillisecondPrecision { get; set; }
        public bool HasRetry { get; set; }
        public bool? IsLastRetry { get; set; }
        public bool IsRetryTrigger { get; set; }
    }
}