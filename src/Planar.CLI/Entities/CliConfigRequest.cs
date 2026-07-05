using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliAddConfigRequest : CliUpdateConfigRequest
{
    [ActionProperty("s", "secret", Name = "secret")]
    public bool IsSecret { get; set; }
}

public class CliUpdateConfigRequest : CliConfigKeyRequest
{
    [ActionProperty(DefaultOrder = 1)]
    public string? Value { get; set; }

    [ActionProperty("u", "url", Name = "url")]
    public string? SourceUrl { get; set; }
}