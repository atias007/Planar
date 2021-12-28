using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace Planner.API.Common.Entities
{
    public class TriggerRowDetails
    {
        [YamlMember(Order = 0)]
        public DateTime? NextFireTime
        {
            get
            {
                var d1 = SimpleTriggers.Where(t => t.NextFireTime.HasValue).Min(t => t.NextFireTime);
                var d2 = CronTriggers.Where(t => t.NextFireTime.HasValue).Min(t => t.NextFireTime);
                var all = new[] { d1, d2 };

                return all.Where(d => d.HasValue).Min();
            }
        }

        [YamlMember(Order = 1)]
        public List<SimpleTriggerDetails> SimpleTriggers { get; set; } = new();

        [YamlMember(Order = 2)]
        public List<CronTriggerDetails> CronTriggers { get; set; } = new();
    }
}