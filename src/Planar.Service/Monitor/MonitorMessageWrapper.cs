using System.Text.Json;

namespace Planar.Service.Monitor
{
    public class MonitorMessageWrapper
    {
        private const string version = "1.0";

        public MonitorMessageWrapper(MonitorDetails details)
        {
            Subject = nameof(MonitorDetails);
            Version = version;
            HandleMonitor(details);
        }

        public MonitorMessageWrapper(MonitorSystemDetails details)
        {
            Subject = nameof(MonitorSystemDetails);
            Version = version;
            HandleMonitor(details);
        }

        public void HandleMonitor<T>(T monitor)
            where T : Monitor
        {
            Users = JsonSerializer.Serialize(monitor.Users);
            Group = JsonSerializer.Serialize(monitor.Group);
            GlobalConfig = JsonSerializer.Serialize(monitor.GlobalConfig);

            monitor.Users = null;
            monitor.Group = null;
            monitor.GlobalConfig = null;

            Details = JsonSerializer.Serialize(monitor);
        }

        public string Version { get; set; }

        public string Subject { get; set; }

        public string Users { get; set; } = null!;

        public string Group { get; set; } = null!;

        public string Details { get; set; } = null!;

        public string GlobalConfig { get; set; } = null!;
    }
}