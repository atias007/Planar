using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliListTriggersRequest : CliJobKey
    {
        [Required("job id argument is required")]
        [ActionProperty(DefaultOrder = 0, Name = "job id")]
        public new string Id { get; set; } = string.Empty;

        [QuietActionProperty]
        public bool Quiet { get; set; }
    }
}