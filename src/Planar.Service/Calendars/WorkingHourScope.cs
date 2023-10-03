using System;

namespace Planar.Service.Calendars;

public class WorkingHourScope
{
    public TimeSpan Start { get; set; }

    public TimeSpan End { get; set; }

    public bool IsTimeIncluded(DateTime dateTime)
    {
        return dateTime.TimeOfDay >= Start && dateTime.TimeOfDay < End;
    }
}