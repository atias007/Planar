using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class BaseTrigger
    {
        [YamlIgnore]
        public string Id { get; set; } = string.Empty;

        [YamlIgnore]
        public string? Group { get; set; }

        public string? Name { get; set; }

        [YamlMember(Alias = "misfire behaviour")]
        public string? MisfireBehaviour { get; set; }

        public int? Priority { get; set; }

        [YamlMember(Alias = "retry span")]
        public TimeSpan? RetrySpan { get; set; }

        [YamlMember(Alias = "max retries")]
        public int? MaxRetries { get; set; }

        [YamlMember(Alias = "trigger data")]
        public Dictionary<string, string?> TriggerData { get; set; } = new Dictionary<string, string?>();

        public string? Calendar { get; set; }

        public TimeSpan? Timeout { get; set; }
    }
}