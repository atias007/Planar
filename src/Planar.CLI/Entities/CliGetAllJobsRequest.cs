using Planar.API.Common.Entities;
using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetAllJobsRequest : IQuietResult, IPagingRequest
    {
        [ActionProperty(DefaultOrder = 0, Name = "group")]
        public string? JobGroup { get; set; }

        [QuietActionProperty]
        public bool Quiet { get; set; }

        [ActionProperty("a", "active")]
        public bool Active { get; set; }

        [ActionProperty("i", "inactive")]
        public bool Inactive { get; set; }

        [ActionProperty("s", "system")]
        public bool System { get; set; }

        [ActionProperty("t", "type")]
        public string? JobType { get; set; }

        public int? PageNumber { get; set; }

        public int? PageSize => 25;

        public void SetPagingDefaults()
        {
            PageNumber ??= 1;
        }
    }
}