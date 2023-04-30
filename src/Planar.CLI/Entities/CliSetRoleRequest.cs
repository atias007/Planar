using Planar.API.Common.Entities;
using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliSetRoleRequest
    {
        // TOCHECK: [Required("group argument is required")]
        [ActionProperty(DefaultOrder = 0, Name = "group")]
        public int GroupId { get; set; }

        // TOCHECK: [Required("role argument is required")]
        [ActionProperty(DefaultOrder = 1, Name = "role")]
        public Roles? Role { get; set; }
    }
}