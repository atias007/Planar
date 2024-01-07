using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliSetHourOfReport : CliDisableReport
    {
        [Required]
        [ActionProperty(DefaultOrder = 2, Name = "hour")]
        public int? HourOfDay { get; set; }
    }
}