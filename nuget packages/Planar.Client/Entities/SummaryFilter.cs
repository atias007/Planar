using System;

namespace Planar.Client.Entities
{
    public class SummaryFilter : PagingRequest, IDateScope
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}