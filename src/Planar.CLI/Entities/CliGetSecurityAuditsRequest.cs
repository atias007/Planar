using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities
{
    public class CliGetSecurityAuditsRequest : CliPagingRequest
    {
        [ActionProperty("f", "from")]
        public DateTime FromDate { get; set; }

        [ActionProperty("a", "asc")]
        public bool Ascending { get; set; }

        [ActionProperty("t", "to")]
        public DateTime ToDate { get; set; }
    }
}