using Microsoft.Extensions.Logging;
using System;

namespace Planar.Service.Calendars
{
    [Serializable]
    public sealed class IsraelCalendar : BasePlanarCalendar
    {
        public const string Name = "israel";

        public IsraelCalendar()
        {
            var calendar = Calendars.WorkingHours.GetCalendar(Name) ?? throw new PlanarCalendarException($"Invalid calendar name '{Name}'");
            WorkingHours = calendar;
        }

        public override bool IsTimeIncluded(DateTimeOffset timeStampUtc)
        {
            var localDate = timeStampUtc.ToLocalTime().DateTime;

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
}