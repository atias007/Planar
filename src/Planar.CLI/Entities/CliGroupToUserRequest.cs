using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGroupToUserRequest
    {
        [ActionProperty(DefaultOrder = 0, Name = "user id")]
        public int UserId { get; set; }

        [ActionProperty(DefaultOrder = 1, Name = "group id")]
        public int GroupId { get; set; }
    }
}