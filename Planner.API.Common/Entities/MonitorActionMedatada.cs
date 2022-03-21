using System.Collections.Generic;

namespace Planner.API.Common.Entities
{
    public class MonitorActionMedatada
    {
        public Dictionary<string, string> Jobs { get; set; }

        public List<string> JobGroups { get; set; }

        public Dictionary<int, string> Events { get; set; }

        public Dictionary<int, string> Groups { get; set; }

        public Dictionary<int, string> Hooks { get; set; }
    }
}