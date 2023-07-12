using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetLastHistoryCallForJobRequest : CliPagingRequest
    {
        [ActionProperty(Default = true, Name = "last days")]
        public int LastDays { get; set; }
    }
}