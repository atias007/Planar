using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliMonitorHookRequest
{
    [Required("monitor id is required")]
    [ActionProperty(DefaultOrder = 0, Name = "monitor id")]
    public int MonitorId { get; set; }

    [Required("hook is required")]
    [ActionProperty(DefaultOrder = 1, Name = "group name")]
    public string Hook { get; set; } = null!;
}