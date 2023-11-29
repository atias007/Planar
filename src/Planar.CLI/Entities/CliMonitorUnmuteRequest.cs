using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliMonitorUnmuteRequest
    {
        [ActionProperty("j", "job")]
        public string? JobId { get; set; }

        [ActionProperty("m", "monitor")]
        public int? MonitorId { get; set; }
    }
}