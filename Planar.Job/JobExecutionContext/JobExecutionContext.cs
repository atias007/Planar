using Planar.Job;
using System;
using System.Collections.Generic;

namespace Planar
{
    internal class JobExecutionContext : IJobExecutionContext
    {
        public Dictionary<string, string> JobSettings { get; set; }

        public SortedDictionary<string, string> MergedJobDataMap { get; set; }

        public string FireInstanceId { get; set; }

        public DateTimeOffset FireTime { get; set; }

        public DateTimeOffset? NextFireTime { get; set; }

        public DateTimeOffset? ScheduledFireTime { get; set; }

        public DateTimeOffset? PreviousFireTime { get; set; }

        public bool Recovering { get; set; }

        public int RefireCount { get; set; }

        public IJobDetail JobDetails { get; set; }

        public ITriggerDetail TriggerDetails { get; set; }

        public string Environment { get; set; }
    }
}