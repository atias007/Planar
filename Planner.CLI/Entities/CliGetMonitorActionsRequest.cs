using Planner.CLI.Attributes;

namespace Planner.CLI.Entities
{
    public class CliGetMonitorActionsRequest
    {
        [ActionProperty(Default = true)]
        public string JobIdOrJobGroup { get; set; }
    }
}