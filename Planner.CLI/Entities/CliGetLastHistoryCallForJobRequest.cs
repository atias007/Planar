using Planner.CLI.Attributes;

namespace Planner.CLI.Entities
{
    public class CliGetLastHistoryCallForJobRequest
    {
        [ActionProperty(Default = true)]
        public int LastDays { get; set; }
    }
}