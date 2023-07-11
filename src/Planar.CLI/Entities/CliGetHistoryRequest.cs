using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities
{
    public class CliGetHistoryRequest : IPagingRequest
    {
        [ActionProperty("r", "rows")]
        public int Rows { get; set; }

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

        public int? PageNumber { get; set; }

        public int? PageSize => 25;

        public void SetPagingDefaults()
        {
            PageNumber ??= 1;
        }
    }
}