using System;
using System.Collections.Generic;

namespace Planar.Monitor.Hook
{
    internal class Monitor : IMonitor
    {
        public int EventId { get; set; }

        public string EventTitle { get; set; } = string.Empty;

        public string MonitorTitle { get; set; } = string.Empty;

        public IMonitorGroup Group { get; set; } = new Group();

        public IEnumerable<IMonitorUser> Users { get; set; } = new List<User>();

        public Dictionary<string, string?> GlobalConfig { get; set; } = new Dictionary<string, string?>();

        public Exception? Exception { get; set; }
    }
}