using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliDisableReport : CliReport
    {
        [Required]
        [ActionProperty(DefaultOrder = 1)]
        public ReportPeriods Period { get; set; }
    }
}