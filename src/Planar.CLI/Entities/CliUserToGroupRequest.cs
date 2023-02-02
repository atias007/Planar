using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliUserToGroupRequest
    {
        [ActionProperty(DefaultOrder = 0, Name = "group id")]
        [Required("group id argument is required")]
        public int GroupId { get; set; }

        [ActionProperty(DefaultOrder = 1, Name = "user id")]
        [Required("user id argument is required")]
        public int UserId { get; set; }
    }
}