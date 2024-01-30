using System;

namespace Planar.Client.Entities
{
    internal class DateScope : IDateScope
    {
        public DateScope(DateTime? fromDate, DateTime? toDate)
        {
            FromDate = fromDate;
            ToDate = toDate;
        }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}