using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Planner.Calendar.Hebrew
{
    public class HebrewCalendarSettings
    {
        private static readonly List<PropertyInfo> _properties = GetProperties();

        public WorkingHours WorkingHours { get; set; }

        private static List<PropertyInfo> GetProperties()
        {
            return typeof(WorkingHours).GetProperties(BindingFlags.Instance | BindingFlags.Public).ToList();
        }

        public DayScope GetDayScope(DayOfWeek dow)
        {
            var caption = dow.ToString();
            var dayPropertyInfo = _properties.First(p => p.Name == caption);
            var scope = dayPropertyInfo.GetValue(WorkingHours) as DayScope;
            return scope;
        }
    }

    public class WorkingHours
    {
        public DayScope Sunday { get; set; }
        public DayScope Monday { get; set; }
        public DayScope Tuesday { get; set; }
        public DayScope Wednesday { get; set; }
        public DayScope Thursday { get; set; }
        public DayScope Friday { get; set; }
        public DayScope HolidayEve { get; set; }
        public DayScope Saturday { get; set; }
        public DayScope Holiday { get; set; }
        public DayScope Sabbaton { get; set; }
    }

    public class DayScope
    {
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
    }
}