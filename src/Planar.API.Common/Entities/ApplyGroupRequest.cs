using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities;

public class ApplyGroupRequest : AddGroupRequest, IApplyRequest
{
    [YamlMember(Alias = "users")]
    public List<string> Users { get; set; } = [];

    // ===== APPLY REQUEST PROPERTIES ===== ////

    [YamlMember(Alias = "source")]
    public string Source { get; set; } = string.Empty;

    [YamlMember(Alias = "kind")]
    public string Kind { get; set; } = string.Empty;

    [YamlMember(Alias = "version")]
    public string Version { get; set; } = string.Empty;

    // ==================================== ////
}