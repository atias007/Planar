using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public abstract class SetJobRequest : ITriggersContainer
    {
        [YamlMember(Alias = "job type")]
        public string? JobType { get; set; }

        public string? Author { get; set; }

        public string? Name { get; set; }

        public string? Group { get; set; }

        public string? Description { get; set; }

        public bool? Durable { get; set; }

        public bool Concurrent { get; set; }

        [YamlMember(Alias = "job data")]
        public Dictionary<string, string?> JobData { get; set; } = new();

        [YamlMember(Alias = "global config")]
        public Dictionary<string, string?> GlobalConfig { get; set; } = new();

        [YamlMember(Alias = "simple triggers")]
        public List<JobSimpleTriggerMetadata> SimpleTriggers { get; set; } = new();

        [YamlMember(Alias = "cron triggers")]
        public List<JobCronTriggerMetadata> CronTriggers { get; set; } = new();
    }
}