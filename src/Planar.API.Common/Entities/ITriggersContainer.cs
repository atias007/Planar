using System.Collections.Generic;

namespace Planar.API.Common.Entities
{
    public interface ITriggersContainer
    {
        List<JobCronTriggerMetadata> CronTriggers { get; set; }
        List<JobSimpleTriggerMetadata> SimpleTriggers { get; set; }
    }
}