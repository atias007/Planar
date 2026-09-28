using Planar.API.Common.Entities;
using YamlDotNet.Serialization;

namespace Planar.Service.Model;

public class ResourceModel
{
    [YamlMember(Alias = "name")]
    public required string Name { get; set; }

    [YamlMember(Alias = "value")]
    public required string Value { get; set; }
}

public class ApplyResourceRequest : ResourceModel, IApplyRequest
{
    // ===== APPLY REQUEST PROPERTIES ===== ////

    [YamlMember(Alias = "source")]
    public string Source { get; set; } = string.Empty;

    [YamlMember(Alias = "kind")]
    public string Kind { get; set; } = string.Empty;

    [YamlMember(Alias = "version")]
    public string Version { get; set; } = string.Empty;

    // ==================================== ////
}