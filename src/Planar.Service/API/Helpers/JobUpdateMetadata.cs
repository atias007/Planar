using Quartz;
using System.Collections.Generic;

namespace Planar.Service.API.Helpers
{
    internal class JobUpdateMetadata
    {
        public IJobDetail JobDetails { get; set; }

        public IReadOnlyCollection<ITrigger> Triggers { get; set; }

        public IJobDetail OldJobDetails { get; set; }

        public IReadOnlyCollection<ITrigger> OldTriggers { get; set; }

        public string OldJobProperties { get; set; }

        public JobKey JobKey { get; set; }

        public string JobId { get; set; }
    }
}