using System;
using System.Collections.Generic;

namespace Planar.MonitorHook
{
    internal class MonitorSystemDetails : Monitor, IMonitorSystemDetails
    {
        public string MessageTemplate { get; set; }

        public string Message { get; set; }

        public IReadOnlyDictionary<string, string> MessagesParameters { get; set; }
    }
}