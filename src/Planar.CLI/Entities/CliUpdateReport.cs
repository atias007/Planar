using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public enum ReportPeriods
    {
        [ActionEnumOption("daily")] Daily,
        [ActionEnumOption("weekly")] Weekly,
        [ActionEnumOption("monthly")] Monthly,
        [ActionEnumOption("quarterly")] Quarterly,
        [ActionEnumOption("yearly")] Yearly
    }

    public class CliUpdateReport : CliReport
    {
        [Required]
        [ActionProperty(DefaultOrder = 1)]
        public ReportPeriods Period { get; set; }

        [ActionProperty("g", "group")]
        public string? Group { get; set; }
    }
}