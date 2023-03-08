using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class CronTriggerDetails : TriggerDetails
    {
        [YamlMember(Order = 96)]
        public string CronExpression { get; set; } = string.Empty;

        [YamlMember(Order = 97)]
        public string CronDescription { get; set; } = string.Empty;
    }
}