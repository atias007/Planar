using System;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class JobSimpleTriggerMetadata : BaseTrigger
    {
        [YamlMember(Alias = "start")]
        public DateTime? Start { get; set; }

        [YamlMember(Alias = "end")]
        public DateTime? End { get; set; }

        public TimeSpan Interval { get; set; }

        [YamlMember(Alias = "repeat count")]
        public int? RepeatCount { get; set; }
    }
}