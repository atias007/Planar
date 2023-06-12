using System;
using System.Collections.Generic;

namespace Planar.Job
{
    internal class JobDetail : IJobDetail
    {
        public IKey Key { get; set; } = null!;

        public string Description { get; set; } = string.Empty;

        public SortedDictionary<string, string?> JobDataMap { get; set; } = new SortedDictionary<string, string?>();

        public bool Durable { get; set; }

        public bool PersistJobDataAfterExecution { get; set; }

        public bool ConcurrentExecutionDisallowed { get; set; }

        public bool RequestsRecovery { get; set; }
    }
}