using Planner.CLI.Attributes;

namespace Planner.CLI.Entities
{
    public class CliGetTriggersDetailsRequest : CliJobOrTriggerKey
    {
        [QuietActionProperty]
        public bool Quiet { get; set; }
    }
}