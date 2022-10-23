using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class JobCronTriggerMetadata : BaseTrigger
    {
        [YamlMember(Alias = "cron expression")]
        public string CronExpression { get; set; }
    }
}