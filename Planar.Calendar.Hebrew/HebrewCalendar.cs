using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using System;
using System.Collections.Generic;

namespace Planar.Calendar.Hebrew
{
    [Serializable]
    public class HebrewCalendar : BaseCalendar<HebrewCalendar>
    {
        private static HebrewCalendarSettings _settings;
        private readonly Dictionary<long, bool> _cache = new();

        public HebrewCalendar(ILogger logger) : base(logger)
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
                Logger.LogCritical(ex, "Fail to invoke IsTimeIncluded with DateTimeOffset={@timeUtc}", timeUtc);
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

        protected void AddCache(DateTimeOffset date, bool result)
        {
            _cache.TryAdd(date.Ticks, result);
        }

        protected bool? GetCache(DateTimeOffset date)
        {
            if (_cache.TryGetValue(date.Ticks, out var result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }
    }

    public class CustomCalendarSerializer : CalendarSerializer<HebrewCalendar>
    {
        private readonly ILogger _logger;

        public CustomCalendarSerializer(ILogger logger)
        {
            _logger = logger;
        }

        protected override HebrewCalendar Create(JObject source)
        {
            return new HebrewCalendar(_logger);
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