using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliConfigRequest : CliConfigKeyRequest
    {
        [ActionProperty(DefaultOrder = 1)]
        [Required("value parameter is required")]
        public string Value { get; set; }

        public string Type { get; set; }
    }
}