using Planner.CLI.Attributes;

namespace Planner.CLI.Entities
{
    public class CliStopScheduler
    {
        [ActionProperty(ShortName = "f", LongName = "force")]
        public bool Force { get; set; }
    }
}