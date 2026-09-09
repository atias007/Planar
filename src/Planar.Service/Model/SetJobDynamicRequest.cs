using Planar.API.Common.Entities;
using YamlDotNet.Serialization;

namespace Planar.Service.Model;

internal class SetJobDynamicRequest : SetJobRequest, IApplyRequest
{
    public virtual dynamic? Properties { get; set; }

    [YamlIgnore]
    public string Source { get; set; } = string.Empty;
}