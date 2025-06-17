using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliMonitorGroupRequest
{
    [Required("monitor id is required")]
    [ActionProperty(DefaultOrder = 0)]
    public int MonitorId { get; set; }

    [Required("group name is required")]
    [ActionProperty(DefaultOrder = 1)]
    public string GroupName { get; set; } = null!;
}