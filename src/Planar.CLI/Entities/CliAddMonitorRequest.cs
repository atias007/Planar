using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliAddMonitorRequest : CliBaseMonitorRequest
{
    [ActionProperty("g", "group")]
    [Required("group argument is required")]
    public string GroupName { get; set; } = null!;

    [ActionProperty("h", "hook")]
    [Required("hook argument is required")]
    public string Hook { get; set; } = null!;
}