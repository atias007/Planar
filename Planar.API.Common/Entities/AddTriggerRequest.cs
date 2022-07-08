using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    // TODO: add validation
    public class AddTriggerRequest : JobOrTriggerKey, ITriggersContainer
    {
        [YamlMember(Alias = "simple triggers")]
        public List<JobSimpleTriggerMetadata> SimpleTriggers { get; set; }

        [YamlMember(Alias = "cron triggers")]
        public List<JobCronTriggerMetadata> CronTriggers { get; set; }
    }
}