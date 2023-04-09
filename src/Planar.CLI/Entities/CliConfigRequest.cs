using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliConfigRequest : CliConfigKeyRequest
    {
        [ActionProperty(DefaultOrder = 1)]
        public string? Value { get; set; }
    }
}