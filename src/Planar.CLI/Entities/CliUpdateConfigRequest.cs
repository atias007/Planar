using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliUpdateConfigRequest : CliConfigKeyRequest
{
    [ActionProperty("v", "value")]
    public string? Value { get; set; }

    [ActionProperty("u", "url")]
    public string? SourceUrl { get; set; }
}