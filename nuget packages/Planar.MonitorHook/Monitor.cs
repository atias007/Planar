using System;
using System.Collections.Generic;

namespace Planar.MonitorHook
{
    internal class Monitor : IMonitor
    {
        public int EventId { get; set; }

        public string EventTitle { get; set; }

        public string MonitorTitle { get; set; }

        public IMonitorGroup Group { get; set; }

        public IEnumerable<IMonitorUser> Users { get; set; }

        public Exception Exception { get; set; }
    }
}