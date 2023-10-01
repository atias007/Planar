using System;

namespace Planar.API.Common.Entities
{
    public class GetTraceRequest : PagingRequest, IDateScope
    {
        public bool Ascending { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string Level { get; set; } = string.Empty;
    }
}