using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliUpdateMonitorRequest : CliBaseMonitorRequest
{
    [ActionProperty("i", "id")]
    [Required("id argument is required")]
    public int Id { get; set; }
}
