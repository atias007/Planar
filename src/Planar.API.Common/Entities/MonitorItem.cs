using System.Collections.Generic;

namespace Planar.API.Common.Entities;

public class MonitorItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public string? JobName { get; set; }
    public string? JobGroup { get; set; }
    public string? EventArgument { get; set; }
    public string Hook { get; set; } = string.Empty;
    public bool Active { get; set; }

    [DisplayFormat(Hide = true)]
    public int EventId { get; set; }

    public IEnumerable<string> DistributionGroups { get; set; } = [];
}