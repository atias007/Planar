using Planner.CLI.Attributes;

namespace Planner.CLI.Entities
{
    public class CliGetMonitorItemsRequest
    {
        [ActionProperty(Default = true)]
        public string JobIdOrJobGroup { get; set; }
    }
}