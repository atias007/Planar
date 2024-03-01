using System;

namespace Planar.Client.Entities
{
    internal class CounterFilter : IDateScope
    {
        public CounterFilter(DateTime? fromDate, DateTime? toDate)
        {
            FromDate = fromDate;
            ToDate = toDate;
        }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}