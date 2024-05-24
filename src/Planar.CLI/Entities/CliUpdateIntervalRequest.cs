using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliUpdateIntervalRequest : CliTriggerKey
    {
        [Required("interval argument is required")]
        [ActionProperty(DefaultOrder = 1)]
        public string Interval { get; set; } = string.Empty;
    }
}