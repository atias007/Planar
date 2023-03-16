using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliFireInstanceIdRequest
    {
        [ActionProperty(DefaultOrder = 0, Name = "fire instance id")]
        [Required]
        public string FireInstanceId { get; set; } = string.Empty;
    }
}