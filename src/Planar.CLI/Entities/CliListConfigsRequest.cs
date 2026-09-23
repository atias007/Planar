using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliListConfigsRequest : CliPagingRequest
    {
        [ActionProperty("f", "flat")]
        public bool Flat { get; set; }
    }
}