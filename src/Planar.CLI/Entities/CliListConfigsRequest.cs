using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliListConfigsRequest
    {
        [ActionProperty("f", "flat")]
        public bool Flat { get; set; }
    }
}