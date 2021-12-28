using Planner.CLI.Attributes;
using System;

namespace Planner.CLI.Entities
{
    public class CliGetHistoryRequest
    {
        [ActionProperty(ShortName = "r", LongName = "rows")]
        public int Rows { get; set; }

        [ActionProperty(ShortName = "f", LongName = "from")]
        public DateTime FromDate { get; set; }

        [ActionProperty(ShortName = "d", LongName = "desc")]
        public bool Ascending { get; set; }

        [ActionProperty(ShortName = "t", LongName = "to")]
        public DateTime ToDate { get; set; }

        [ActionProperty(ShortName = "s", LongName = "status")]
        public string Status { get; set; }
    }
}