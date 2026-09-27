using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities;

public class GlobalConfigModelUpdateRequest
{
    [YamlMember(Alias = "key")]
    public required string Key { get; set; }

    [YamlMember(Alias = "value")]
    public string? Value { get; set; }

    [YamlMember(Alias = "source url")]
    public string? SourceUrl { get; set; }
}

public class GlobalConfigModelAddRequest : GlobalConfigModelUpdateRequest
{
    [YamlIgnore]
    public string? Type { get; set; }

    [YamlMember(Alias = "is secret")]
    public bool? IsSecret { get; set; }
}

public class GlobalConfigApplyRequest : GlobalConfigModelAddRequest, IApplyRequest
{
    //// ===== APPLY REQUEST PROPERTIES ===== ////

    [YamlMember(Alias = "source")]
    public string Source { get; set; } = string.Empty;

    [YamlMember(Alias = "kind")]
    public string Kind { get; set; } = string.Empty;

    [YamlMember(Alias = "version")]
    public string Version { get; set; } = string.Empty;

    //// ==================================== ////
}