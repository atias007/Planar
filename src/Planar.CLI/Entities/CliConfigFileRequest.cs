using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliConfigFileRequest : CliConfigKeyRequest
    {
        [ActionProperty(DefaultOrder = 1)]
        [Required("filename argument is required")]
        public string Filename { get; set; } = null!;
    }
}