using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities;

public abstract class MonitorRequest
{
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = null!;

    [YamlMember(Alias = "job name")]
    public string? JobName { get; set; }

    [YamlMember(Alias = "job group")]
    public string? JobGroup { get; set; }

    [YamlMember(Alias = "event")]
    public string Event { get; set; } = null!;

    [YamlIgnore]
    public string? EventArgument { get; set; }
}