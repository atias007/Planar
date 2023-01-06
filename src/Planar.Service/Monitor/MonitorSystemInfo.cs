using System.Collections.Generic;

namespace Planar.Service.Monitor
{
    public class MonitorSystemInfo
    {
        public string MessageTemplate { get; set; }

        public Dictionary<string, string> MessagesParameters { get; private set; } = new();
    }
}