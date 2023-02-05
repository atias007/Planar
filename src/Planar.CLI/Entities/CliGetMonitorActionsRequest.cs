using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetMonitorActionsRequest
    {
        [ActionProperty(Default = true)]
        public string? JobIdOrJobGroup { get; set; }
    }
}