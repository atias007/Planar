using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliUserToGroupRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        public string Name { get; set; } = null!;

        [ActionProperty(DefaultOrder = 1, Name = "group name")]
        public string GroupName { get; set; } = null!;
    }
}