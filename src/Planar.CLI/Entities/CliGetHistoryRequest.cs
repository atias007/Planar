using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities
{
    public class CliGetHistoryRequest : CliPagingRequest
    {
        [ActionProperty("f", "from")]
        public DateTime FromDate { get; set; }

        [ActionProperty("a", "asc")]
        public bool Ascending { get; set; }

        [ActionProperty("t", "to")]
        public DateTime ToDate { get; set; }

        [ActionProperty("s", "status")]
        public StatusMembers? Status { get; set; }

        [ActionProperty("j", "job")]
        public string? JobId { get; set; }

        [ActionProperty("jt", "job-type")]
        public string? JobType { get; set; }

        [ActionProperty("jg", "job-group")]
        public string? JobGroup { get; set; }

        [ActionProperty("o", "outlier")]
        public bool? Outlier { get; set; }
    }
}