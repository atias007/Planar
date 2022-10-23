using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliUpsertJobPropertyRequest : CliJobOrTriggerKey
    {
        [ActionProperty(DefaultOrder = 1)]
        [Required("key parameter is required")]
        public string PropertyKey { get; set; }

        [ActionProperty(DefaultOrder = 2)]
        [Required("value parameter is required")]
        public string PropertyValue { get; set; }
    }
}