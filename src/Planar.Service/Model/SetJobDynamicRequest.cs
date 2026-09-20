using Planar.API.Common.Entities;
using YamlDotNet.Serialization;

namespace Planar.Service.Model;

internal class SetJobDynamicRequest : SetJobRequest, IApplyRequest
{
    public virtual dynamic? Properties { get; set; }

    //// ===== APPLY REQUEST PROPERTIES ===== ////

    [YamlMember(Alias = "source")]
    public string Source { get; set; } = string.Empty;

    [YamlMember(Alias = "kind")]
    public string Kind { get; set; } = string.Empty;

    [YamlMember(Alias = "version")]
    public string Version { get; set; } = string.Empty;

    //// ==================================== ////
}