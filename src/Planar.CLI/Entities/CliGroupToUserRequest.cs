using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGroupToUserRequest
    {
        [ActionProperty(DefaultOrder = 0, Name = "group name")]
        public string Name { get; set; } = null!;

        [ActionProperty(DefaultOrder = 1)]
        public string Username { get; set; } = null!;
    }
}