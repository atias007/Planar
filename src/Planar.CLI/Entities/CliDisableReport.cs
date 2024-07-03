using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliDisableReport : CliReport
    {
        [Required("period argument is required")]
        [ActionProperty(DefaultOrder = 1)]
        public ReportPeriods? Period { get; set; }
    }
}