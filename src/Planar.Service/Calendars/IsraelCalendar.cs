using Microsoft.Extensions.Logging;
using Quartz;
using System;

namespace Planar.Service.Calendars;

[Serializable]
public sealed class IsraelCalendar : BasePlanarCalendar, ICalendar
{
    public const string Name = "israel";

    public IsraelCalendar()
    {
        var calendar = Calendars.WorkingHours.GetCalendar(Name) ?? throw new PlanarCalendarException($"Invalid calendar name '{Name}'");
        WorkingHours = calendar;
    }

    public ICalendar? CalendarBase { set; get; }

    public string? Description { get; set; }

    public ICalendar Clone()
    {
        return new IsraelCalendar();
    }

    public DateTimeOffset GetNextIncludedTimeUtc(DateTimeOffset timeUtc)
    {
        var counter = 0;
        var max = 60 * 24 * 365 * 2; // min * hours * days * years
        do
        {
            counter++;
            var include = IsTimeIncluded(timeUtc);
            if (include) { return timeUtc; }
            timeUtc = timeUtc.AddMinutes(1);
        }
        while (counter < max);

        return DateTimeOffset.MaxValue;
    }

    public bool IsTimeIncluded(DateTimeOffset timeUtc)
    {
        var localDate = timeUtc.ToLocalTime().DateTime;

        try
        {
            return IsWorkingDateTime(localDate);
        }
        catch (Exception ex)
        {
            Log(LogLevel.Critical, ex, "Fail to invoke IsTimeIncluded with locate date/time={TimeStamp}", localDate);
            return false;
        }
    }

    private static WorkingHoursDayType Convert(HebrewEventInfo eventInfo, DateTime dateTime)
    {
        if (eventInfo.IsHoliday)
        {
            return WorkingHoursDayType.PublicHoliday;
        }
        else if (eventInfo.IsHolidayEve)
        {
            return WorkingHoursDayType.PublicHolidayEve;
        }
        else if (eventInfo.IsSabbaton)
        {
            return WorkingHoursDayType.AuthoritiesHoliday;
        }
        else if (eventInfo.IsHolHamoed)
        {
            return WorkingHoursDayType.OptionalHoliday;
        }

        return Convert(dateTime.DayOfWeek);
    }

    private bool IsWorkingDateTime(DateTime date)
    {
        var hebrewDate = HebrewEvent.GetHebrewEventInfo(date);
        var dayType = Convert(hebrewDate, date);
        return IsWorkingDateTime(dayType, date);
    }
}