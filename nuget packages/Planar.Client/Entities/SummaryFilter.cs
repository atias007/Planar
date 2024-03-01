using System;

namespace Planar.Client.Entities
{
    internal class SummaryFilter : Paging, IDateScope
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}