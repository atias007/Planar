using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliAddJobRequest
    {
        [ActionProperty(DefaultOrder = 0, Name = "file")]
        [Required("filename argument is required")]
        public string Filename { get; set; } = string.Empty;
    }
}