using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliFireInstanceIdRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        [Required("instance id parameter is required")]
        public string FireInstanceId { get; set; }
    }
}