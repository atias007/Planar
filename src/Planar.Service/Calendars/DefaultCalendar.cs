using Microsoft.Extensions.Logging;
using Quartz;
using System;

namespace Planar.Service.Calendars;

[Serializable]
public sealed class DefaultCalendar : BasePlanarCalendar, ICalendar
{
    public const string Name = "default";

    public DefaultCalendar()
    {
        var calendar = Calendars.WorkingHours.GetCalendar(Name) ?? throw new PlanarCalendarException($"Invalid calendar name '{Name}'");
        WorkingHours = calendar;
    }

    public string? Description { get; set; }

    public ICalendar? CalendarBase { get; set; }

    public ICalendar Clone()
    {
        return new DefaultCalendar();
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
            var dow = Convert(localDate.DayOfWeek);
            return IsWorkingDateTime(dow, localDate);
        }
        catch (Exception ex)
        {
            Log(LogLevel.Critical, ex, "Fail to invoke IsTimeIncluded with locate date/time={TimeStamp}", localDate);
            return false;
        }
    }
}