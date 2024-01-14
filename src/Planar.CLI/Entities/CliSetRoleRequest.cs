using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliSetRoleRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        [Required("name argument is required")]
        public string Name { get; set; } = null!;

        [ActionProperty(DefaultOrder = 1)]
        [Required("role argument is required")]
        public string? Role { get; set; }
    }
}