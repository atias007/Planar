using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities
{
    public class CliMonitorMuteRequest : CliMonitorUnmuteRequest
    {
        [ActionProperty("ts", "timespan", Name = CliActionMetadata.TimeSpan)]
        [Required("timespan argument is required")]
        public TimeSpan TimeSpan { get; set; }
    }
}