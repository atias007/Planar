using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliSetHourOfReport : CliDisableReport
    {
        [Required("hour of day argument is required")]
        [ActionProperty(DefaultOrder = 2, Name = "hour")]
        public int? HourOfDay { get; set; }
    }
}