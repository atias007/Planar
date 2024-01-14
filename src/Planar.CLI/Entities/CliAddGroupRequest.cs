using Planar.API.Common.Entities;
using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliAddGroupRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        [Required("name argument is required")]
        public string Name { get; set; } = string.Empty;

        [ActionProperty(DefaultOrder = 1, Name = "role")]
        [Required("role argument is required")]
        public Roles? Role { get; set; }
    }
}