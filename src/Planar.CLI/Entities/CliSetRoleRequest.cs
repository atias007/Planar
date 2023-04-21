using Planar.API.Common.Entities;
using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliSetRoleRequest
    {
        [ActionProperty(DefaultOrder = 0, Name = "group")]
        [Required("group id argument is required")]
        public int GroupId { get; set; }

        [ActionProperty(DefaultOrder = 1, Name = "role")]
        [Required("role argument is required")]
        public Roles Role { get; set; }
    }
}