using System;
using System.Collections.Generic;

namespace Planar.Job.Test
{
    internal class ExecuteJobProperties
    {
        public Type? JobType { get; set; }

        public TimeSpan? CancelJobAfter { get; set; }

        public DateTimeOffset? ExecutionDate { get; set; }

        public bool Recovering { get; set; }

        public int RefireCount { get; set; }

        public string? Environment { get; set; }

        public Dictionary<string, object?> TriggerData { get; set; } = new Dictionary<string, object?>();

        public Dictionary<string, object?> JobData { get; set; } = new Dictionary<string, object?>();

        public Dictionary<string, string?> GlobalSettings { get; set; } = new Dictionary<string, string?>();
    }
}