using System.Text.Json;

namespace Planar.Service.Monitor;

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
        Groups = JsonSerializer.Serialize(monitor.Groups);
        GlobalConfig = JsonSerializer.Serialize(monitor.GlobalConfig);

        monitor.Groups = [];
        monitor.GlobalConfig = [];

        Details = JsonSerializer.Serialize(monitor);
    }

    public string Version { get; set; }

    public string Subject { get; set; }

    public string Groups { get; set; } = null!;

    public string Details { get; set; } = null!;

    public string GlobalConfig { get; set; } = null!;
}