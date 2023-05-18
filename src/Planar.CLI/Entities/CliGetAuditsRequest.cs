using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetAuditsRequest : IPagingRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        public uint PageNumber { get; set; }

        public byte PageSize => 10;
    }
}