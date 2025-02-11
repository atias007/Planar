using System.Collections.Generic;

namespace Planar.Client.Entities
{
    public class JobDetails : JobBasicDetails
    {
#if NETSTANDARD2_0
        public string Author { get; set; }

#else
        public string? Author { get; set; }

#endif
        public int? LogRetentionDays { get; set; }
        public bool Durable { get; set; }
        public bool RequestsRecovery { get; set; }
        public bool Concurrent { get; set; }
        public string Properties { get; set; } = string.Empty;
#if NETSTANDARD2_0
        public Dictionary<string, string> DataMap { get; set; } = new Dictionary<string, string>();

#else
        public Dictionary<string, string?> DataMap { get; set; } = new Dictionary<string, string?>();

#endif

        public List<SimpleTriggerDetails> SimpleTriggers { get; set; } = new List<SimpleTriggerDetails>();
        public List<CronTriggerDetails> CronTriggers { get; set; } = new List<CronTriggerDetails>();

#if NETSTANDARD2_0
        public JobCircuitBreaker CircuitBreaker { get; set; }
#else
        public JobCircuitBreaker? CircuitBreaker { get; set; }
#endif
    }
}