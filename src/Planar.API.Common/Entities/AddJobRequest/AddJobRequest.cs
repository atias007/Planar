using System.Collections.Generic;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class AddJobRequest : ITriggersContainer
    {
        [YamlMember(Alias = "job type")]
        public string JobType { get; set; }

        public string Name { get; set; }

        public string Group { get; set; }

        public string Description { get; set; }

        public bool? Durable { get; set; }

        public bool Concurrent { get; set; }

        [YamlMember(Alias = "job data")]
        public Dictionary<string, string> JobData { get; set; }

        [JsonIgnore]
        public virtual dynamic Properties { get; set; }

        [YamlMember(Alias = "global config")]
        public Dictionary<string, string> GlobalConfig { get; set; }

        [YamlMember(Alias = "simple triggers")]
        public List<JobSimpleTriggerMetadata> SimpleTriggers { get; set; }

        [YamlMember(Alias = "cron triggers")]
        public List<JobCronTriggerMetadata> CronTriggers { get; set; }
    }
}