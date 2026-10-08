using Planar.API.Common.Entities;
using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliAddUrlConfigRequest : CliConfigKeyRequest
{
    [Required("source url argument is required")]
    [ActionProperty(DefaultOrder = 1, Name ="url")]
    public string? SourceUrl { get; set; }

    [ActionProperty("t", "type")]
    public GlobalConfigTypes? Type { get; set; }
}
