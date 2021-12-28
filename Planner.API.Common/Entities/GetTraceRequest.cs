using System;

namespace Planner.API.Common.Entities
{
    public class GetTraceRequest
    {
        public int? Rows { get; set; }

        public bool Ascending { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string Level { get; set; }
    }
}