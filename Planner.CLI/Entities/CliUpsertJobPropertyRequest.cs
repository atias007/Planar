using Planner.CLI.Attributes;

namespace Planner.CLI.Entities
{
    public class CliUpsertJobPropertyRequest : CliJobOrTriggerKey
    {
        [ActionProperty(DefaultOrder = 1)]
        public string PropertyKey { get; set; }

        [ActionProperty(DefaultOrder = 2)]
        public string PropertyValue { get; set; }
    }
}