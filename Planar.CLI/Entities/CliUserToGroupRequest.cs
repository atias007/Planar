using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliUserToGroupRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        public int GroupId { get; set; }

        [ActionProperty(DefaultOrder = 1)]
        public int UserId { get; set; }
    }
}