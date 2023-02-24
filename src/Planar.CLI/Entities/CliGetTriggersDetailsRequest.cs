using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetTriggersDetailsRequest : CliTriggerKey
    {
        [QuietActionProperty]
        public bool Quiet { get; set; }
    }
}