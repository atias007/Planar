using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliUpdateConfigRequest : CliConfigKeyRequest
{
    [ActionProperty("v", "value", InputDisplay = "value")]
    public string? Value { get; set; }

    [ActionProperty("u", "url", InputDisplay = "url")]
    public string? SourceUrl { get; set; }
}