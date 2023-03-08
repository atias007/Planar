using System;
using System.Collections.Generic;

namespace Planar.Monitor.Hook
{
    public interface IMonitor
    {
        int EventId { get; }
        string EventTitle { get; }
        string MonitorTitle { get; }
        IMonitorGroup Group { get; }
        IEnumerable<IMonitorUser> Users { get; }
        Dictionary<string, string?> GlobalConfig { get; }
        Exception? Exception { get; }
    }
}