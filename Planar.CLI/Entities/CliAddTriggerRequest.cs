using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliAddTriggerRequest : CliJobOrTriggerKey
    {
        [ActionProperty(DefaultOrder = 1)]
        public string Filename { get; set; }
    }
}