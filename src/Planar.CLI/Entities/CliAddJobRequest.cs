using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliAddJobRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        [Required]
        public string Folder { get; set; } = string.Empty;
    }
}