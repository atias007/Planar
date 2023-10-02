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
            IEnumerable<WorkingHourScope>? scopes;

            if (hebrewDate.IsHoliday)
            {
                scopes = _calendar.GetDay(WorkingHoursDayType.PublicHoliday)?.Scopes;
            }
            else if (hebrewDate.IsHolidayEve)
            {
                scopes = _calendar.GetDay(WorkingHoursDayType.PublicHolidayEve)?.Scopes;
            }
            else if (hebrewDate.IsSabbaton)
            {
                scopes = _calendar.GetDay(WorkingHoursDayType.AuthoritiesHoliday)?.Scopes;
            }
            else
            {
                switch (date.DayOfWeek)
                {
                    case DayOfWeek.Sunday:
                        scopes = _calendar.GetDay(WorkingHoursDayType.Sunday)?.Scopes;
                        break;

                    case DayOfWeek.Monday:
                        scopes = _calendar.GetDay(WorkingHoursDayType.Monday)?.Scopes;
                        break;

                    case DayOfWeek.Tuesday:
                        scopes = _calendar.GetDay(WorkingHoursDayType.Tuesday)?.Scopes;
                        break;

                    case DayOfWeek.Wednesday:
                        scopes = _calendar.GetDay(WorkingHoursDayType.Wednesday)?.Scopes;
                        break;

                    case DayOfWeek.Thursday:
                        scopes = _calendar.GetDay(WorkingHoursDayType.Thursday)?.Scopes;
                        break;

                    case DayOfWeek.Friday:
                        scopes = _calendar.GetDay(WorkingHoursDayType.Friday)?.Scopes;
                        break;

                    case DayOfWeek.Saturday:
                        scopes = _calendar.GetDay(WorkingHoursDayType.Saturday)?.Scopes;
                        break;
                }
            }

            var validHours = date >= start && date <= end;

            return validHours;
        }
    }
}