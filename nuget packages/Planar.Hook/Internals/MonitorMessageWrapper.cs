using System.Text.Json;

namespace Planar.Hook
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
            Groups = JsonSerializer.Serialize(monitor.Groups);
            GlobalConfig = JsonSerializer.Serialize(monitor.GlobalConfig);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            monitor.ClearGroups();
            monitor.GlobalConfig = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            Details = JsonSerializer.Serialize(monitor);
        }

#if NETSTANDARD2_0
        public string Version { get; set; }
        public string Subject { get; set; }
        public string Groups { get; set; }
        public string Details { get; set; }
        public string GlobalConfig { get; set; }
#else

        public string Version { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string Groups { get; set; } = null!;
        public string Details { get; set; } = null!;
        public string GlobalConfig { get; set; } = null!;
#endif
    }
}