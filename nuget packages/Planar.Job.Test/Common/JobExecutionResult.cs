using System;
using System.Collections.Generic;

namespace Planar.Job.Test
{
    internal class JobExecutionResult : IJobExecutionResult
    {
        public int Id { get; set; }
        public string InstanceId { get; set; }
        public string JobId { get; set; }
        public string JobName { get; set; }
        public string JobGroup { get; set; }
        public string TriggerId { get; set; }
        public string TriggerName { get; set; }
        public string TriggerGroup { get; set; }
        public StatusMembers Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Duration { get; set; }
        public int? EffectedRows { get; set; }
        public bool Retry { get; set; }
        public bool IsStopped { get; set; }
        public IReadOnlyDictionary<string, string> Data { get; set; }
        public string Log { get; set; }
        public Exception Exception { get; set; }

        public AssertPlanarConstraint Assert => new AssertPlanarConstraint(this);
    }
}