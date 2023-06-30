using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class PausedTriggerDetails
    {
        [YamlMember(Order = 0)]
        public string Id { get; set; } = string.Empty;

        [YamlMember(Order = 1)]
        public string TriggerName { get; set; } = string.Empty;

        [YamlMember(Order = 2)]
        public string JobId { get; set; } = string.Empty;

        [YamlMember(Order = 3)]
        public string JobName { get; set; } = string.Empty;

        [YamlMember(Order = 4)]
        public string JobGroup { get; set; } = string.Empty;
    }
}