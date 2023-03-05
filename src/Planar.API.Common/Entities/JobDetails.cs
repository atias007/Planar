using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class JobDetails : JobRowDetails
    {
        [YamlMember(Order = 10)]
        public string Author { get; set; }

        [YamlMember(Order = 11)]
        public bool Durable { get; set; }

        [YamlMember(Order = 12)]
        public bool RequestsRecovery { get; set; }

        [YamlMember(Order = 13)]
        public bool Concurrent { get; set; }

        [YamlMember(Order = 14)]
        public string Properties { get; set; }

        [YamlMember(Order = 15)]
        public SortedDictionary<string, string> DataMap { get; set; } = new();

        [YamlMember(Order = 16)]
        public List<SimpleTriggerDetails> SimpleTriggers { get; set; } = new();

        [YamlMember(Order = 17)]
        public List<CronTriggerDetails> CronTriggers { get; set; } = new();
    }
}