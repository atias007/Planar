using YamlDotNet.Serialization;

namespace Planar.Service.Model.Metadata
{
    public class JobCronTriggerMetadata : BaseTrigger
    {
        [YamlMember(Alias = "cron expression")]
        public string CronExpression { get; set; }
    }
}