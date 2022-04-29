using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class JobDetails : JobRowDetails
    {
        [YamlMember(Order = 4)]
        public bool Durable { get; set; }

        [YamlMember(Order = 5)]
        public bool RequestsRecovery { get; set; }

        [YamlMember(Order = 6)]
        public bool ConcurrentExecution { get; set; }

        [YamlMember(Order = 7)]
        public SortedDictionary<string, string> Properties { get; set; } = new();

        [YamlMember(Order = 8)]
        public SortedDictionary<string, string> DataMap { get; set; } = new();

        [YamlMember(Order = 9)]
        public List<SimpleTriggerDetails> SimpleTriggers { get; set; } = new();

        [YamlMember(Order = 10)]
        public List<CronTriggerDetails> CronTriggers { get; set; } = new();
    }
}