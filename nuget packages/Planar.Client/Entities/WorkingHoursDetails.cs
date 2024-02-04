using System;
using System.Collections.Generic;

namespace Planar.Client.Entities
{
    public class WorkingHoursDetails
    {
        public string CalendarName { get; set; } = null!;
        public List<WorkingHoursDayDetails> Days { get; set; } = new List<WorkingHoursDayDetails>();
    }

    public class WorkingHoursDayDetails
    {
        public string DayOfWeek { get; set; } = null!;
        public List<WorkingHourScopeDetails> Scopes { get; set; } = null!;
    }

    public class WorkingHourScopeDetails
    {
        public TimeSpan Start { get; set; }

        public TimeSpan End { get; set; }
    }
}