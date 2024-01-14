using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetWorkingHoursRequest
    {
        [Required]
        [ActionProperty(DefaultOrder = 0)]
        public string? Calendar { get; set; }
    }
}