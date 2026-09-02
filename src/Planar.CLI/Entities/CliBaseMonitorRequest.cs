using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliBaseMonitorRequest
{
    [ActionProperty("t", "title")]
    [Required("title argument is required")]
    public string Title { get; set; } = string.Empty;

    [ActionProperty("jn", "job-name")]
    public string? JobName { get; set; }

    [ActionProperty("jg", "job-group")]
    public string? JobGroup { get; set; }

    [ActionProperty("e", "event")]
    [Required("event argument is required")]
    public string Event { get; set; } = null!;

    [ActionProperty("ax", "arguments-x")]
    public int? EventArgumentX { get; set; }

    [ActionProperty("ay", "arguments-y")]
    public int? EventArgumentY { get; set; }

    [ActionProperty("v", "active")]
    public bool Active { get; set; }
}