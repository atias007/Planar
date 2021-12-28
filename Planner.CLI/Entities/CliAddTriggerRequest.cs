using Planner.CLI.Attributes;

namespace Planner.CLI.Entities
{
    public class CliAddTriggerRequest : CliJobOrTriggerKey
    {
        [ActionProperty(DefaultOrder = 1)]
        public string Filename { get; set; }
    }
}