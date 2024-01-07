using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliSetAuthorOfJob : CliJobKey
    {
        [Required("author argument is required")]
        [ActionProperty(DefaultOrder = 1)]
        public string? Author { get; set; }
    }
}