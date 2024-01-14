using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities
{
    public class CliGetHistorySummaryRequest : CliPagingRequest, ICliDateScope
    {
        [ActionProperty("f", "from")]
        public DateTime FromDate { get; set; }

        [ActionProperty("t", "to")]
        public DateTime ToDate { get; set; }
    }
}