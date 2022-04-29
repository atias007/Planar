using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliStopScheduler
    {
        [ActionProperty(ShortName = "f", LongName = "force")]
        public bool Force { get; set; }
    }
}