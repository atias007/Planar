using System;
using System.Collections.Generic;

namespace Planar.Monitor.Hook
{
    internal class MonitorSystemDetails : Monitor, IMonitorSystemDetails
    {
        public string MessageTemplate { get; set; }

        public string Message { get; set; }

        public IReadOnlyDictionary<string, string> MessagesParameters { get; set; }
    }
}