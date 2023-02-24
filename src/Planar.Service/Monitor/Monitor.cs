using System;
using System.Collections.Generic;

namespace Planar.Service.Monitor
{
    public class Monitor
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; }
        public string MonitorTitle { get; set; }
        public MonitorGroup Group { get; set; }
        public List<MonitorUser> Users { get; set; } = new();
        public Exception Exception { get; set; }
        public Dictionary<string, string> GlobalConfig { get; set; }
    }
}