using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities
{
    public class CliMonitorMuteRequest : CliMonitorUnmuteRequest
    {
        [ActionProperty("ts", "timespan")]
        public TimeSpan TimeSpan { get; set; }
    }
}