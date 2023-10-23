using System;

namespace Planar.Service.Reports
{
    public class DateScope
    {
        public DateScope(DateTime from, DateTime to, string period)
        {
            From = from;
            To = to;
            Period = period;
        }

        public DateScope(DateTime from, DateTime to, ReportPeriods period)
        {
            From = from;
            To = to;
            Period = period.ToString();
        }

        public DateTime From { get; private set; }
        public DateTime To { get; private set; }
        public string Period { get; private set; }
    }
}