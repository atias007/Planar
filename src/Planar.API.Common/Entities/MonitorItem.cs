using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Planar.API.Common.Entities;

public class MonitorItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public string? JobName { get; set; }
    public string? JobGroup { get; set; }
    public string DistributionGroupNames { get; set; } = string.Empty;
    public string? EventArgument { get; set; }
    public string Hook { get; set; } = string.Empty;
    public bool Active { get; set; }

    [DisplayFormat(Hide = true)]
    public int EventId { get; set; }

    private IEnumerable<string> _groupNames = [];

    [JsonIgnore]
    [DisplayFormat(Hide = true)]
    public IEnumerable<string> GroupNames
    {
        get { return _groupNames; }
        set
        {
            _groupNames = value;
            DistributionGroupNames = string.Join(",", GroupNames);
        }
    }
}