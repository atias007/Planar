using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliConfigKeyRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        [Required("key argument is required")]
        public string Key { get; set; } = null!;
    }
}