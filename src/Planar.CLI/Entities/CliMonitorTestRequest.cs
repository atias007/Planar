using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliMonitorTestRequest
    {
        [ActionProperty(DefaultOrder = 1)]
        [Required("hook argument is required")]
        public string Hook { get; set; } = string.Empty;

        [ActionProperty(DefaultOrder = 2, Name = "event")]
        [Required("event argument is required")]
        public string EventName { get; set; } = null!;

        [ActionProperty(DefaultOrder = 3, Name = "group name")]
        [Required("group id argument is required")]
        public string GroupName { get; set; } = null!;
    }
}