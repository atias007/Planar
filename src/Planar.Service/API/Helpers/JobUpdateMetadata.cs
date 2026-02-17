using Quartz;
using System.Collections.Generic;

namespace Planar.Service.API.Helpers
{
    internal class JobUpdateMetadata
    {
        public IJobDetail JobDetails { get; set; } = null!;

        public IReadOnlyCollection<ITrigger> Triggers { get; set; } = null!;

        public IReadOnlyCollection<TriggerKey> PausedTriggers { get; set; } = null!;

        public IJobDetail OldJobDetails { get; set; } = null!;

        public IReadOnlyCollection<ITrigger> OldTriggers { get; set; } = null!;

        public string? OldJobProperties { get; set; }

        public JobKey JobKey { get; set; } = null!;

        public string JobId { get; set; } = null!;

        public string? Author { get; set; }

        public int? LogRetentionDays { get; set; }

        public bool RollbackEnabled { get; private set; }

        public void EnableRollback()
        {
            RollbackEnabled = true;
        }
    }
}