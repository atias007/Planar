using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities
{
    public class CliGetTraceRequest : CliPagingRequest
    {
        [ActionProperty(ShortName = "a", LongName = "asc")]
        public bool Ascending { get; set; }

        [ActionProperty(ShortName = "f", LongName = "from")]
        public DateTime FromDate { get; set; }

        [ActionProperty(ShortName = "t", LongName = "to")]
        public DateTime ToDate { get; set; }

        [ActionProperty(ShortName = "l", LongName = "level")]
        public string? Level { get; set; }
    }
}