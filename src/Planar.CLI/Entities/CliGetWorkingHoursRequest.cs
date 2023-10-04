using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetWorkingHoursRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        public string? Calendar { get; set; }
    }
}