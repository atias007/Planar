using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliListTriggersRequest : CliJobKey
    {
        [QuietActionProperty]
        public bool Quiet { get; set; }
    }
}