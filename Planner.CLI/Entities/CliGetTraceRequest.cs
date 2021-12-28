using Planner.CLI.Attributes;
using System;

namespace Planner.CLI.Entities
{
    public class CliGetTraceRequest
    {
        [ActionProperty(ShortName = "r", LongName = "rows")]
        public int Rows { get; set; }

        [ActionProperty(ShortName = "a", LongName = "asc")]
        public bool Ascending { get; set; }

        [ActionProperty(ShortName = "f", LongName = "from")]
        public DateTime FromDate { get; set; }

        [ActionProperty(ShortName = "t", LongName = "to")]
        public DateTime ToDate { get; set; }

        [ActionProperty(ShortName = "l", LongName = "level")]
        public string Level { get; set; }
    }
}