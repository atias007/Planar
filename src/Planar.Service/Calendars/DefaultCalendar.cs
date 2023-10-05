using Microsoft.Extensions.Logging;
using System;

namespace Planar.Service.Calendars
{
    [Serializable]
    public sealed class DefaultCalendar : BasePlanarCalendar
    {
        public const string Name = "default";

        public DefaultCalendar()
        {
            var calendar = Calendars.WorkingHours.GetCalendar(Name) ?? throw new PlanarCalendarException($"Invalid calendar name '{Name}'");
            WorkingHours = calendar;
        }

        public override bool IsTimeIncluded(DateTimeOffset timeStampUtc)
        {
            var localDate = timeStampUtc.ToLocalTime().DateTime;

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
}