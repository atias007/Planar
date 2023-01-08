using System.Collections.Generic;

namespace Planar.Service.Monitor
{
    public class MonitorSystemDetails : Monitor
    {
        public string MessageTemplate { get; set; }

        public string Message { get; set; }

        public Dictionary<string, string> MessagesParameters { get; set; }
    }
}