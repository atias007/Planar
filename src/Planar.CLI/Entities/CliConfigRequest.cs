using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliAddConfigRequest : CliConfigKeyRequest
{
    [Required("value argument is required")]
    [ActionProperty(DefaultOrder = 1)]
    public string? Value { get; set; }
}