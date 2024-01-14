using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliReport
    {
        [Required("report argument is required")]
        [ActionProperty(DefaultOrder = 0)]
        public string Report { get; set; } = null!;
    }
}