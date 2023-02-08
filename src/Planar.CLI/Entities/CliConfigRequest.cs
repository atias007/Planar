using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliConfigRequest : CliConfigKeyRequest
    {
        [ActionProperty(DefaultOrder = 1)]
        [Required("value argument is required")]
        public string Value { get; set; } = string.Empty;
    }
}