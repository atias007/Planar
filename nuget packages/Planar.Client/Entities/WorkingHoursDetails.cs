using System;
using System.Collections.Generic;

namespace Planar.Client.Entities
{
    public class WorkingHoursDetails
    {
#if NETSTANDARD2_0
        public string CalendarName { get; set; }
#else
        public string CalendarName { get; set; } = null!;
#endif

        public List<WorkingHoursDayDetails> Days { get; set; } = new List<WorkingHoursDayDetails>();
    }

    public class WorkingHoursDayDetails
    {
#if NETSTANDARD2_0
        public string DayOfWeek { get; set; }
        public List<WorkingHourScopeDetails> Scopes { get; set; }
#else
        public string DayOfWeek { get; set; } = null!;
        public List<WorkingHourScopeDetails> Scopes { get; set; } = null!;
#endif
    }

    public class WorkingHourScopeDetails
    {
        public TimeSpan Start { get; set; }

        public TimeSpan End { get; set; }
    }
}