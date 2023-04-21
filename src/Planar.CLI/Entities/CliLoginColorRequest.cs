using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliLoginColorRequest
    {
        [ActionProperty("c", "color", DefaultOrder = 0)]
        public CliColors? Color { get; set; }
    }
}