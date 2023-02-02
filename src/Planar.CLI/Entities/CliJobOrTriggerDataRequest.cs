using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public enum JobDataActions
    {
        [ActionEnumOption("upsert")]
        Upsert,

        [ActionEnumOption("remove")]
        Remove
    }

    public class CliJobOrTriggerDataRequest : CliJobOrTriggerKey
    {
        [ActionProperty(DefaultOrder = 1, Name = "action")]
        [Required("action argument is required")]
        public JobDataActions Action { get; set; }

        [ActionProperty(DefaultOrder = 2, Name = "key")]
        public string? DataKey { get; set; }

        [ActionProperty(DefaultOrder = 3, Name = "value")]
        public string? DataValue { get; set; }
    }
}