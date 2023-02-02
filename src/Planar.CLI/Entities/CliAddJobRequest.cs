using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliAddJobRequest
    {
        [ActionProperty(DefaultOrder = 0, Name = "folder")]
        [Required("folder argument is required")]
        public string Folder { get; set; } = string.Empty;
    }
}