using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class PausedTriggerDetails
    {
        [YamlMember(Order = 0)]
        public string Id { get; set; }

        [YamlMember(Order = 1)]
        public string Name { get; set; }

        [YamlMember(Order = 2)]
        public string Group { get; set; }

        [YamlMember(Order = 3)]
        public string Description { get; set; }

        [YamlMember(Order = 4)]
        public string JobId { get; set; }
    }
}