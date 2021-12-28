using Planner.CLI.Attributes;

namespace Planner.CLI.Entities
{
    public class CliUserToGroupRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        public int GroupId { get; set; }

        [ActionProperty(DefaultOrder = 1)]
        public int UserId { get; set; }
    }
}