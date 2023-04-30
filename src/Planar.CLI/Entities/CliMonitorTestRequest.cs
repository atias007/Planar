using Planar.API.Common.Entities;
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
        public TestMonitorEvents MonitorEvent { get; set; }

        [ActionProperty(DefaultOrder = 3, Name = "group id")]
        [Required("group id argument is required")]
        public int DistributionGroupId { get; set; }
    }
}