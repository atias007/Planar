using System.Collections.Generic;

namespace Planar.API.Common.Entities
{
    public class JobDetails : JobBasicDetails
    {
        public string? Author { get; set; }

        public int? LogRetentionDays { get; set; }

        public bool Durable { get; set; }

        public bool RequestsRecovery { get; set; }

        public bool Concurrent { get; set; }

        public string Properties { get; set; } = string.Empty;

        public SortedDictionary<string, string?> DataMap { get; set; } = [];

        public List<SimpleTriggerDetails> SimpleTriggers { get; set; } = [];

        public List<CronTriggerDetails> CronTriggers { get; set; } = [];

        public JobCircuitBreaker? CircuitBreaker { get; set; }
    }
}