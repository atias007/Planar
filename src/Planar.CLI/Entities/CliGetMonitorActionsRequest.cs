using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetMonitorActionsRequest
    {
        [ActionProperty(Default = true, Name = "job id | job group")]
        public string? JobIdOrJobGroup { get; set; }
    }
}