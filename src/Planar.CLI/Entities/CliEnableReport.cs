using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliEnableReport : CliDisableReport
    {
        [ActionProperty("g", "group")]
        public string? Group { get; set; }
    }
}