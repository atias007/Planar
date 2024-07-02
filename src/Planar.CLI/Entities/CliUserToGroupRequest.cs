using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliUserToGroupRequest
    {
        [Required("name argument is required")]
        [ActionProperty(DefaultOrder = 0)]
        public string Name { get; set; } = null!;

        [Required("group name argument is required")]
        [ActionProperty(DefaultOrder = 1, Name = "group name")]
        public string GroupName { get; set; } = null!;
    }
}