using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetLastHistoryCallForJobRequest : CliPagingRequest
    {
        [ActionProperty(Default = true, Name = "last days")]
        public int LastDays { get; set; }

        [ActionProperty("j", "job")]
        public string? JobId { get; set; }

        [ActionProperty("jt", "job-type")]
        public string? JobType { get; set; }

        [ActionProperty("jg", "job-group")]
        public string? JobGroup { get; set; }
    }
}