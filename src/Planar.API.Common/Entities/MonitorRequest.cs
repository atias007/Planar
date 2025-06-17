namespace Planar.API.Common.Entities;

public abstract class MonitorRequest
{
    public string Title { get; set; } = null!;

    public string? JobName { get; set; }

    public string? JobGroup { get; set; }

    public string Event { get; set; } = null!;

    public string? EventArgument { get; set; }

    public string Hook { get; set; } = null!;
}
