using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities;

public class AddGroupRequest
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    [YamlMember(Alias = "additional field 1")]
    public string? AdditionalField1 { get; set; }

    [YamlMember(Alias = "additional field 2")]
    public string? AdditionalField2 { get; set; }

    [YamlMember(Alias = "additional field 3")]
    public string? AdditionalField3 { get; set; }

    [YamlMember(Alias = "additional field 4")]
    public string? AdditionalField4 { get; set; }

    [YamlMember(Alias = "additional field 5")]
    public string? AdditionalField5 { get; set; }

    [YamlMember(Alias = "role")]
    public string? Role { get; set; }
}