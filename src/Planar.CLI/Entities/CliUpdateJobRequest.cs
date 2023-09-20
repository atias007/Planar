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

    public class CliUpdateJobRequest : CliAddJobRequest
    {
        [ActionProperty(DefaultOrder = 1, Name = "options")]
        public JobUpdateOptions? Options { get; set; }
    }
}