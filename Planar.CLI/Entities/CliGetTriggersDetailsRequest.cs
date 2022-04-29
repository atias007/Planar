using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetTriggersDetailsRequest : CliJobOrTriggerKey
    {
        [QuietActionProperty]
        public bool Quiet { get; set; }
    }
}