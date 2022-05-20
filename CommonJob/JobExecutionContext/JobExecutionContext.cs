using System;
using System.Collections.Generic;

namespace CommonJob
{
    internal class JobExecutionContext
    {
        public Dictionary<string, string> JobSettings { get; set; }

        public Dictionary<string, string> MergedJobDataMap { get; set; }

        public string FireInstanceId { get; set; }

        public DateTimeOffset FireTime { get; set; }

        public DateTimeOffset? NextFireTime { get; set; }

        public DateTimeOffset? ScheduledFireTime { get; set; }

        public DateTimeOffset? PreviousFireTime { get; set; }

        public bool Recovering { get; set; }

        public int RefireCount { get; set; }

        public JobDetail JobDetails { get; set; }

        public Key RecoveringTriggerKey { get; set; }

        public string Environment { get; set; }
    }
}