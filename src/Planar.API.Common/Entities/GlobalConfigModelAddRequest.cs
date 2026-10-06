using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities;

public class GlobalConfigModelRequest
{
    [YamlMember(Alias = "type")]
    public string? Type { get; set; }

    [YamlMember(Alias = "is secret")]
    public bool? IsSecret { get; set; }

    [YamlMember(Alias = "key")]
    public required string Key { get; set; }

    [YamlMember(Alias = "value")]
    public string? Value { get; set; }

    [YamlMember(Alias = "source url")]
    public string? SourceUrl { get; set; }
}