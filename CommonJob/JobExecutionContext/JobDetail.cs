using System.Collections.Generic;

namespace CommonJob
{
    public class JobDetail
    {
        public Key Key { get; set; }

        public string Description { get; set; }

        public SortedDictionary<string, string> JobDataMap { get; set; }

        public bool Durable { get; set; }

        public bool PersistJobDataAfterExecution { get; set; }

        public bool ConcurrentExecutionDisallowed { get; set; }

        public bool RequestsRecovery { get; set; }
    }
}