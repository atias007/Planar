using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities
{
    public class CliGetHistoryRequest
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
        public string? Status { get; set; }

        [ActionProperty("j", "job")]
        public string? JobId { get; set; }

        [ActionProperty("jt", "job-type")]
        public string? JobType { get; set; }

        [ActionProperty("jg", "job-group")]
        public string? JobGroup { get; set; }
    }
}