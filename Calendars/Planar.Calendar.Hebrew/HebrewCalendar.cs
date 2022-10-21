using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using System;
using System.Collections.Generic;

namespace Planar.Calendar.Hebrew
{
    [Serializable]
    public class HebrewCalendar : PlanarBaseCalendar
    {
        private HebrewCalendarSettings _settings;
        private static readonly object Locker = new();
        private readonly Dictionary<long, bool> _cache = new();

        public HebrewCalendar()
        {
            _settings ??= LoadSettings<HebrewCalendarSettings>();
        }

        public HebrewCalendarSettings Settings
        {
            get
            {
                lock (Locker)
                {
                    _settings ??= LoadSettings<HebrewCalendarSettings>();

                    return _settings;
                }
            }
        }

        public override bool IsTimeIncluded(DateTimeOffset timeStampUtc)
        {
            try
            {
                var cache = GetCache(timeStampUtc);
                if (cache.HasValue) { return cache.Value; }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: Fail to invoke IsTimeIncluded (try get cache value). {ex.Message}");
                throw;
            }

            try
            {
                return IsWorkingDateTime(timeStampUtc.DateTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fail to invoke IsTimeIncluded with DateTimeOffset={timeStampUtc}. {ex.Message}");
                throw;
            }
        }

        private bool IsWorkingDateTime(DateTime date)
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
        public CustomCalendarSerializer()
        {
        }

        protected override HebrewCalendar Create(JObject source)
        {
            return new HebrewCalendar();
        }

        protected override void SerializeFields(JsonWriter writer, HebrewCalendar calendar)
        {
        }

        protected override void DeserializeFields(HebrewCalendar calendar, JObject source)
        {
        }
    }
}