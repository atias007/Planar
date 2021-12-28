using YamlDotNet.Serialization;

namespace Planner.Service.Model.Metadata
{
    public class JobCronTriggerMetadata : BaseTrigger
    {
        [YamlMember(Alias = "cron expression")]
        public string CronExpression { get; set; }
    }
}