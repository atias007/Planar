using System.Collections.Generic;

namespace Planar.Monitor.Hook
{
    internal class MonitorSystemDetails : Monitor, IMonitorSystemDetails
    {
        public string MessageTemplate { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public IReadOnlyDictionary<string, string?> MessagesParameters { get; set; } = new Dictionary<string, string?>();
    }
}