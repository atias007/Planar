using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliUpdateValueConfigRequest : CliConfigKeyRequest
{
    [ActionProperty(DefaultOrder = 1)]
    [Required("value argument is required")]
    public string? Value { get; set; } = string.Empty;
}

public class CliUpdateUrlConfigRequest : CliConfigKeyRequest
{
    [ActionProperty(DefaultOrder = 1)]
    [Required("url argument is required")]
    public string? Url { get; set; } = string.Empty;
}