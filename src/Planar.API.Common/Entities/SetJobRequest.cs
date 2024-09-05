using System.Collections.Generic;
using YamlDotNet.Core;
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

        [YamlMember(Alias = "log retention days")]
        public int? LogRetentionDays { get; set; }

        [YamlMember(Alias = "job data")]
        public Dictionary<string, string?> JobData { get; set; } = [];

        [YamlMember(Alias = "simple triggers")]
        public List<JobSimpleTriggerMetadata> SimpleTriggers { get; set; } = [];

        [YamlMember(Alias = "cron triggers")]
        public List<JobCronTriggerMetadata> CronTriggers { get; set; } = [];

        [YamlMember(Alias = "circuit breaker")]
        public JobCircuitBreakerMetadata CircuitBreaker { get; set; } = new();
    }
}