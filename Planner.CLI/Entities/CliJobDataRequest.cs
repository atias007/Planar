using Planner.CLI.Attributes;

namespace Planner.CLI.Entities
{
    public enum JobDataActions
    {
        upsert,
        remove,
        clear
    }

    public class CliJobDataRequest : CliJobOrTriggerKey
    {
        [ActionProperty(DefaultOrder = 1)]
        public JobDataActions Action { get; set; }

        [ActionProperty(DefaultOrder = 2)]
        public string DataKey { get; set; }

        [ActionProperty(DefaultOrder = 3)]
        public string DataValue { get; set; }
    }
}