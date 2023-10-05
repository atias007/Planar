using System;
using System.Collections.Generic;

namespace Planar.Service.Calendars;

public static class WorkingHours
{
    public static List<WorkingHoursCalendar> Calendars { get; set; } = new();

    public static WorkingHoursCalendar? GetCalendar(string name)
    {
        var calendar = Calendars.Find(c => string.Equals(c.CalendarName, name, StringComparison.OrdinalIgnoreCase));

        return calendar;
    }
}