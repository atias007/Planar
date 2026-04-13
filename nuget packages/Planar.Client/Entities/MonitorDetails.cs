using System.Collections.Generic;

namespace Planar.Client.Entities
{
    public class MonitorDetails
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Event { get; set; } = string.Empty;
        public int EventId { get; set; }

#if NETSTANDARD2_0
        public string JobName { get; set; }
        public string JobGroup { get; set; }
        public string EventArgument { get; set; }
#else
        public string? JobName { get; set; }
        public string? JobGroup { get; set; }
        public string? EventArgument { get; set; }
#endif

        public IEnumerable<string> Hooks { get; set; } = new List<string>();
        public bool Active { get; set; }
        public IEnumerable<string> DistributionGroups { get; set; } = new List<string>();
    }
}