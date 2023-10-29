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
}