using System;
using System.Collections.Generic;

namespace Planar.Service.Calendars;

public static class WorkingHours
{
    public static List<WorkingHoursCalendar> Calendars { get; set; } = new();

    public static WorkingHoursCalendar? GetCalendar(string name)
    {
        var calendar = Calendars.Find(c =>
            string.Equals(c.Calendar, "default", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.Calendar, name, StringComparison.OrdinalIgnoreCase));

        return calendar;
    }
}