using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class JobRowDetails
    {
        [YamlMember(Order = 0)]
        public string Id { get; set; }

        [YamlMember(Order = 1)]
        public string Group { get; set; }

        [YamlMember(Order = 2)]
        public string Name { get; set; }

        [YamlMember(Order = 3)]
        public string JobType { get; set; }

        [YamlMember(Order = 4)]
        public string Description { get; set; }
    }
}