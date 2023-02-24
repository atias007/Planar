using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliJobDataRequest : CliJobKey
    {
        [ActionProperty(DefaultOrder = 1, Name = "action")]
        [Required("action argument is required")]
        public DataActions Action { get; set; }

        [ActionProperty(DefaultOrder = 2, Name = "key")]
        public string DataKey { get; set; } = string.Empty;

        [ActionProperty(DefaultOrder = 3, Name = "value")]
        public string? DataValue { get; set; }
    }
}