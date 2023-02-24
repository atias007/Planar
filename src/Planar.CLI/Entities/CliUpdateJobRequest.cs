using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public enum JobUpdateOptions
    {
        [ActionEnumOption("all")] All,
        [ActionEnumOption("all-job")] AllJob,
        [ActionEnumOption("all-trigger")] AllTrigger,
        [ActionEnumOption("job-details")] JobDetails,
        [ActionEnumOption("job-data")] JobData,
        [ActionEnumOption("properties")] Properties,
        [ActionEnumOption("triggers")] Triggers,
        [ActionEnumOption("triggers-data")] TriggersData
    }

    public class CliUpdateJobRequest : CliAddJobRequest
    {
        [ActionProperty(DefaultOrder = 1)]
        public JobUpdateOptions? Options { get; set; }
    }
}