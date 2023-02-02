using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliUpsertJobPropertyRequest : CliJobOrTriggerKey
    {
        [ActionProperty(DefaultOrder = 1, Name = "key")]
        [Required("key argument is required")]
        public string PropertyKey { get; set; } = string.Empty;

        [ActionProperty(DefaultOrder = 2, Name = "value")]
        [Required("value argument is required")]
        public string PropertyValue { get; set; } = string.Empty;
    }
}