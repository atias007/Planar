using System;
using System.Collections.Generic;

namespace Planar.Job
{
    internal class JobExecutionContext : IJobExecutionContext
    {
        public Dictionary<string, string?> JobSettings { get; set; } = new Dictionary<string, string?>();

        public IDataMap MergedJobDataMap { get; set; } = new DataMap();

        public string FireInstanceId { get; set; } = string.Empty;

        public DateTimeOffset FireTime { get; set; }

        public DateTimeOffset? NextFireTime { get; set; }

        public DateTimeOffset? ScheduledFireTime { get; set; }

        public DateTimeOffset? PreviousFireTime { get; set; }

        public bool Recovering { get; set; }

        public int RefireCount { get; set; }

        public int JobPort { get; set; }

        public IJobDetail JobDetails { get; set; } = new JobDetail();

        public ITriggerDetail TriggerDetails { get; set; } = new TriggerDetail();

        public string Environment { get; set; } = string.Empty;
    }
}