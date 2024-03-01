using System;
using System.Collections.Generic;
using System.Linq;

namespace Planar.Client.Entities
{
    public class TriggerBasicDetails
    {
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

        public List<SimpleTriggerDetails> SimpleTriggers { get; set; } = new List<SimpleTriggerDetails>();

        public List<CronTriggerDetails> CronTriggers { get; set; } = new List<CronTriggerDetails>();
    }
}