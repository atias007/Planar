using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliAddHookjRequest
    {
        [ActionProperty(DefaultOrder = 1)]
        [Required("filename argument is required")]
        public string Filename { get; set; } = string.Empty;
    }
}