using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliFireInstanceIdRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        [Required("instance id argument is required")]
        public string FireInstanceId { get; set; }
    }
}