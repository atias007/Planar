using System.Collections.Generic;

namespace Planar.Client.Entities
{
    public class JobDetails : JobBasicDetails
    {
        public string? Author { get; set; }

        public int? LogRetentionDays { get; set; }

        public bool Durable { get; set; }

        public bool RequestsRecovery { get; set; }

        public bool Concurrent { get; set; }

        public string Properties { get; set; } = string.Empty;

        public Dictionary<string, string?> DataMap { get; set; } = new Dictionary<string, string?>();

        public List<SimpleTriggerDetails> SimpleTriggers { get; set; } = new List<SimpleTriggerDetails>();

        public List<CronTriggerDetails> CronTriggers { get; set; } = new List<CronTriggerDetails>();
    }
}