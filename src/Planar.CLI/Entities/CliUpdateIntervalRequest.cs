using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities
{
    public class CliUpdateIntervalRequest : CliTriggerKey
    {
        [Required("interval argument is required")]
        [ActionProperty(DefaultOrder = 1, Name = CliActionMetadata.TimeSpan)]
        public TimeSpan Interval { get; set; }
    }
}