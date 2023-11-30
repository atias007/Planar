using System.Text.Json;

namespace Planar.Monitor.Hook
{
    internal class MonitorMessageWrapper
    {
        private const string version = "1.0";

        public MonitorMessageWrapper()
        {
        }

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

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            monitor.Users = null;
            monitor.Group = null;
            monitor.GlobalConfig = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            Details = JsonSerializer.Serialize(monitor);
        }

        public string Version { get; set; } = null!;

        public string Subject { get; set; } = null!;

        public string Users { get; set; } = null!;

        public string Group { get; set; } = null!;

        public string Details { get; set; } = null!;

        public string GlobalConfig { get; set; } = null!;
    }
}