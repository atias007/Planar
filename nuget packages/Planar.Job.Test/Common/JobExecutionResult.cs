using System;
using System.Collections.Generic;

namespace Planar.Job.Test
{
    internal class JobExecutionResult : IJobExecutionResult
    {
        public int Id { get; set; }
        public string InstanceId { get; set; } = string.Empty;
        public string JobId { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public string JobGroup { get; set; } = string.Empty;
        public string TriggerId { get; set; } = string.Empty;
        public string TriggerName { get; set; } = string.Empty;
        public string TriggerGroup { get; set; } = string.Empty;
        public StatusMembers Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Duration { get; set; }
        public int? EffectedRows { get; set; }
        public bool Retry { get; set; }
        public bool IsCanceled { get; set; }
#if NETSTANDARD2_0
        public IReadOnlyDictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
        public string Log { get; set; }
        public Exception Exception { get; set; }
        public object Instance { get; set; }
#else
        public IReadOnlyDictionary<string, string?> Data { get; set; } = new Dictionary<string, string?>();
        public string? Log { get; set; }
        public Exception? Exception { get; set; }
        public object? Instance { get; set; }
#endif

        public AssertPlanarConstraint Assert => new AssertPlanarConstraint(this);

        public static JobExecutionResult Empty => new JobExecutionResult();
    }
}