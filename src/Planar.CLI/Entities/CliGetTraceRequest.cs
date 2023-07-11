using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities
{
    public class CliGetTraceRequest : IPagingRequest
    {
        [ActionProperty(ShortName = "a", LongName = "asc")]
        public bool Ascending { get; set; }

        [ActionProperty(ShortName = "f", LongName = "from")]
        public DateTime FromDate { get; set; }

        [ActionProperty(ShortName = "t", LongName = "to")]
        public DateTime ToDate { get; set; }

        [ActionProperty(ShortName = "l", LongName = "level")]
        public string? Level { get; set; }

        public int? PageNumber { get; set; }

        public int? PageSize => 25;

        public void SetPagingDefaults()
        {
            PageNumber ??= 1;
        }
    }
}