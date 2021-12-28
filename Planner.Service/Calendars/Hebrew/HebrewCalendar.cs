using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using System;

namespace Planner.Service.Calendars.Hebrew
{
    [Serializable]
    public class HebrewCalendar : BaseCalendar<HebrewCalendar>
    {
        private static HebrewCalendarSettings _settings;

        public HebrewCalendar()
        {
            if (_settings == null)
            {
                _settings = LoadSettings<HebrewCalendarSettings>();
            }
        }

        public override bool IsTimeIncluded(DateTimeOffset timeUtc)
        {
            try
            {
                var cache = GetCache(timeUtc);
                if (cache.HasValue) { return cache.Value; }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, $"Fail to invoke IsTimeIncluded (try get cache value)");
                throw;
            }

            try
            {
                return IsWorkingDateTime(timeUtc.DateTime);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, $"Fail to invoke IsTimeIncluded with DateTimeOffset={timeUtc}");
                throw;
            }
        }

        private static bool IsWorkingDateTime(DateTime date)
        {
            var hebrewDate = HebrewEvent.GetHebrewEventInfo(date);
            DateTime start;
            DateTime end;

            if (hebrewDate.IsHoliday)
            {
                start = GetStartOfDay(date, _settings.WorkingHours.Holiday);
                end = GetEndOfDay(date, _settings.WorkingHours.Holiday);
            }
            else if (hebrewDate.IsHolidayEve)
            {
                start = GetStartOfDay(date, _settings.WorkingHours.HolidayEve);
                end = GetEndOfDay(date, _settings.WorkingHours.HolidayEve);
            }
            else if (hebrewDate.IsSabbaton)
            {
                start = GetStartOfDay(date, _settings.WorkingHours.Sabbaton);
                end = GetEndOfDay(date, _settings.WorkingHours.Sabbaton);
            }
            else
            {
                var scope = _settings.GetDayScope(date.DayOfWeek);
                start = GetStartOfDay(date, scope);
                end = GetEndOfDay(date, scope);
            }

            var validHours = date >= start && date <= end;

            return validHours;
        }

        private static DateTime GetStartOfDay(DateTime date, DayScope scope)
        {
            var result = date.Date;
            if (scope.StartTime.HasValue)
            {
                result = result.Add(scope.StartTime.Value);
            }

            return result;
        }

        private static DateTime GetEndOfDay(DateTime date, DayScope scope)
        {
            var result = date.Date;
            if (scope.EndTime.HasValue)
            {
                result = result.Add(scope.EndTime.Value);
            }
            else
            {
                result = result.AddDays(1).AddMilliseconds(-1);
            }

            return result;
        }
    }

    public class CustomCalendarSerializer : CalendarSerializer<HebrewCalendar>
    {
        protected override HebrewCalendar Create(JObject source)
        {
            return new HebrewCalendar();
        }

        protected override void SerializeFields(JsonWriter writer, HebrewCalendar calendar)
        {
            //writer.WritePropertyName("SomeCustomProperty");
            //writer.WriteValue(calendar.SomeCustomProperty);
        }

        protected override void DeserializeFields(HebrewCalendar calendar, JObject source)
        {
            //calendar.SomeCustomProperty = source["SomeCustomProperty"]!.Value<bool>();
        }
    }
}