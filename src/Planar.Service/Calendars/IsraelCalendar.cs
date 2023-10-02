using Quartz.Impl.Calendar;
using System;
using System.Collections.Generic;

namespace Planar.Service.Calendars
{
    [Serializable]
    public class IsraelCalendar : BaseCalendar
    {
        public const string Name = "Israel";

        private static readonly Dictionary<long, bool> _cache = new();
        private readonly WorkingHoursCalendar _calendar;

        public IsraelCalendar()
        {
            var calendar = WorkingHours.GetCalendar(Name) ?? throw new PlanarCalendarException($"Invalid calendar name {Name}");
            _calendar = calendar;
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
                return false;
            }
        }

        private static bool? GetCache(DateTimeOffset date)
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

        private bool IsWorkingDateTime(DateTime date)
        {
            var hebrewDate = HebrewEvent.GetHebrewEventInfo(date);
            IEnumerable<WorkingHourScope> scopes;

            if (hebrewDate.IsHoliday)
            {
                scopes = _calendar.;
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
    }
}