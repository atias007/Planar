using Microsoft.Extensions.Logging;
using Nager.Date;
using Nager.Date.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Planar.Service.Calendars
{
    [Serializable]
    public sealed class GlobalCalendar : BasePlanarCalendar
    {
        private string _calendarCode = string.Empty;
        private readonly Dictionary<string, IEnumerable<PublicHoliday>> _publicHolidays = new();
        private readonly object _lock = new();

        private string? _name;

        public string? Name
        {
            get { return _name; }
            set
            {
                _name = value;
                if (string.IsNullOrWhiteSpace(_name)) { return; }
                var calendar = Calendars.WorkingHours.GetCalendar(_name) ?? throw new PlanarCalendarException($"Invalid calendar name '{_name}'");
                WorkingHours = calendar;
                _calendarCode = CalendarInfo.GetCalendarCode(_name);
            }
        }

        public string? Key { get; set; }

        public override bool IsTimeIncluded(DateTimeOffset timeStampUtc)
        {
            var localDate = timeStampUtc.ToLocalTime().DateTime;

            try
            {
                var holidays = GetHolidays(localDate);
                foreach (var holiday in holidays)
                {
                    var dayType = Convert(holiday);
                    var isWorking = IsWorkingDateTime(dayType, localDate);
                    if (isWorking) { return true; }
                }

                var dayowType = Convert(localDate.DayOfWeek);
                var result = IsWorkingDateTime(dayowType, localDate);
                return result;
            }
            catch (Exception ex)
            {
                Log(LogLevel.Critical, ex, "Fail to invoke IsTimeIncluded with locate date/time={TimeStamp}", localDate);
                return false;
            }
        }

        private static WorkingHoursDayType Convert(PublicHoliday holiday)
        {
            return holiday.Type switch
            {
                PublicHolidayType.Public => WorkingHoursDayType.PublicHoliday,
                PublicHolidayType.Bank => WorkingHoursDayType.BankHoliday,
                PublicHolidayType.Authorities => WorkingHoursDayType.AuthoritiesHoliday,
                PublicHolidayType.Optional => WorkingHoursDayType.OptionalHoliday,
                PublicHolidayType.Observance => WorkingHoursDayType.ObservanceHoliday,
                _ => WorkingHoursDayType.None,
            };
        }

        private IEnumerable<PublicHoliday> GetHolidays(DateTime dateTime)
        {
            var list = GetHolidays(dateTime.Year)
                .Where(l => l.Date.Date == dateTime.Date)
                .ToList();

            return list;
        }

        private IEnumerable<PublicHoliday> GetHolidays(int year)
        {
            var key = year.ToString();

            if (_publicHolidays.ContainsKey(key)) { return _publicHolidays[key]; }

            lock (_lock)
            {
                if (_publicHolidays.ContainsKey(key)) { return _publicHolidays[key]; }

                var result =
                    DateSystem.GetPublicHolidays(year, _calendarCode)
                    .Where(h => h.Type != PublicHolidayType.School);

                _publicHolidays.Add(key, result);
                return result;
            }
        }
    }
}