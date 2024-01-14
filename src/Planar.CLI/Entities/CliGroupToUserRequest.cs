using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGroupToUserRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        [Required("name argument is required")]
        public string Name { get; set; } = null!;

        [ActionProperty(DefaultOrder = 1)]
        [Required("username argument is required")]
        public string Username { get; set; } = null!;
    }
}