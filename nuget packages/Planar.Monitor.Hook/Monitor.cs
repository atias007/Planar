using System;
using System.Collections.Generic;

namespace Planar.Monitor.Hook
{
    internal class Monitor : IMonitor
    {
        public int EventId { get; set; }

        public string EventTitle { get; set; }

        public string MonitorTitle { get; set; }

        public IMonitorGroup Group { get; set; }

        public IEnumerable<IMonitorUser> Users { get; set; }

        public Dictionary<string, string> GlobalConfig { get; set; }

        public Exception Exception { get; set; }
    }
}