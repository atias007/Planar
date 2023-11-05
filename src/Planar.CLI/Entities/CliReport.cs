using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliReport
    {
        [Required]
        [ActionProperty(DefaultOrder = 0)]
        public string Report { get; set; } = null!;
    }
}