using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities;

public class ApplyMonitorRequest : MonitorRequest
{
    [YamlMember(Alias = "kind")]
    public string Kind { get; set; } = string.Empty;

    [YamlMember(Alias = "distribution groups")]
    public List<string> DistributionGroups { get; set; } = [];

    [YamlMember(Alias = "hooks")]
    public List<string> Hooks { get; set; } = [];

    [YamlMember(Alias = "active")]
    public bool Active { get; set; }
}