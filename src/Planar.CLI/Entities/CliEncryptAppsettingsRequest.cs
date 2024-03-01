using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliEncryptAppsettingsRequest
    {
        [Required("filename argument is required")]
        [ActionProperty(Default = true)]
        public string? Filename { get; set; }
    }
}