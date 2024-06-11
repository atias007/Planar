using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public enum JobUpdateOptions
    {
        [ActionEnumOption("no-data")] NoData,
        [ActionEnumOption("job-data")] JobData,
        [ActionEnumOption("triggers-data")] TriggersData,
        [ActionEnumOption("all")] All,
    }

    public class CliUpdateJobRequest
    {
        [ActionProperty(DefaultOrder = 0, Name = "filename")]
        [Required("filename argument is required")]
        public string Filename { get; set; } = string.Empty;

        [ActionProperty(DefaultOrder = 1, Name = "options")]
        [Required("options argument is required")]
        public JobUpdateOptions? Options { get; set; }
    }
}