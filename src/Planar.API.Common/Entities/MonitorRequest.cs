using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
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

    [YamlMember(Alias = "event arguments")]
    public MonitorEventArguments? EventArguments { get; set; }

    public bool HasEventArgument([NotNullWhen(true)] out MonitorEventArguments? eventArgument)
    {
        eventArgument = EventArguments;
        return EventArguments != null && !EventArguments.IsEmpty();
    }

    public string? ToEventArgumentString()
    {
        if (!HasEventArgument(out var eventArgument)) { return null; }
        var serializer = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        var yaml = serializer.Serialize(eventArgument);
        if (!string.IsNullOrWhiteSpace(yaml)) { yaml = yaml.Trim(); }
        return yaml;
    }
}

public class MonitorEventArguments
{
    [YamlMember(Alias = "x")]
    public int? X { get; set; }

    [YamlMember(Alias = "y")]
    public int? Y { get; set; }

    public bool IsEmpty() => !X.HasValue && !Y.HasValue;
}