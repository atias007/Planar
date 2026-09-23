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

    [YamlIgnore]
    public bool? IsSecret { get; set; }
}

public class GlobalConfigApplyRequest : GlobalConfigModelUpdateRequest, IApplyRequest
{
    public string Kind { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    public string Source { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    public string Version { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
}