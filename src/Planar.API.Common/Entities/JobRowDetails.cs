using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class JobRowDetails
    {
        [YamlMember(Order = 0)]
        public string Id { get; set; } = string.Empty;

        [YamlMember(Order = 1)]
        public string Group { get; set; } = string.Empty;

        [YamlMember(Order = 2)]
        public string Name { get; set; } = string.Empty;

        [YamlMember(Order = 3)]
        public string JobType { get; set; } = string.Empty;

        [YamlMember(Order = 4)]
        public string? Description { get; set; }
    }
}