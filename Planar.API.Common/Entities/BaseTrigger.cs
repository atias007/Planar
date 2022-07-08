using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class BaseTrigger
    {
        public string Name { get; set; }

        public string Group { get; set; }

        [YamlMember(Alias = "misfire behaviour")]
        public int? MisfireBehaviour { get; set; }

        public int? Priority { get; set; }

        [YamlMember(Alias = "retry span")]
        public TimeSpan? RetrySpan { get; set; }

        [YamlMember(Alias = "trigger data")]
        public Dictionary<string, string> TriggerData { get; set; }

        public string Calendar { get; set; }
    }
}