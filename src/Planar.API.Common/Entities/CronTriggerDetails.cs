using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class CronTriggerDetails : TriggerDetails
    {
        [YamlMember(Order = 40)]
        public string CronExpression { get; set; } = string.Empty;

        [YamlMember(Order = 41)]
        public string CronDescription { get; set; } = string.Empty;
    }
}