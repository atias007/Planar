using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities;

public class ApplyPasswordRequest : SetPasswordRequest, IApplyRequest
{
    [YamlMember(Alias = "username")]
    public string? Username { get; set; }

    // ===== APPLY REQUEST PROPERTIES ===== ////

    [YamlMember(Alias = "source")]
    public string Source { get; set; } = string.Empty;

    [YamlMember(Alias = "kind")]
    public string Kind { get; set; } = string.Empty;

    [YamlMember(Alias = "version")]
    public string Version { get; set; } = string.Empty;

    // ==================================== ////
}