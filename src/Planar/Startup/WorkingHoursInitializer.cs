using Planar.Common;
using Planar.Service.Calendars;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Planar.Startup
{
    public static class WorkingHoursInitializer
    {
        public static void Initialize()
        {
            var file = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, "WorkingHours.yml");
            try
            {
                Console.WriteLine("[x] Read WorkingHours file");
                var yml = File.ReadAllText(file);
                WorkingHours.Calendars = YmlUtil.Deserialize<List<WorkingHoursCalendar>>(yml);
                FixInitialize();
                Validate();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Fail to read working hours settings file:");
                Console.WriteLine(ex.Message);
                Thread.Sleep(60000);
                Console.ReadLine();
                Environment.Exit(-1);
            }
        }

        private static void FixInitialize()
        {
            WorkingHours.Calendars ??= new List<WorkingHoursCalendar>();
            WorkingHours.Calendars.ForEach(c =>
            {
                c.CalendarName = c.CalendarName?.ToLower();
                c.DefaultScopes ??= new List<WorkingHourScope>();
                c.DefaultScopes = c.DefaultScopes.OrderBy(s => s.Start).ToList();
                c.Days ??= new List<WorkingHoursDay>();
                c.Days = c.Days.OrderBy(d => d.DayOfWeekMember).ToList();
                c.Days.ForEach(d =>
                {
                    d.DayOfWeek = d.DayOfWeek?.ToLower();
                    d.Scopes ??= new List<WorkingHourScope>();
                    d.Scopes = d.Scopes.OrderBy(s => s.Start).ToList();
                });
            });
        }

        private static void Validate()
        {
            // duplicate calendars
            var names = WorkingHours.Calendars.Select(c => c.CalendarName).ToList();
            if (names.Count != names.Distinct().Count())
            {
                throw new PlanarCalendarException($"calendar name is duplicated");
            }

            foreach (var calendar in WorkingHours.Calendars)
            {
                ValidateCalendar(calendar);
            }
        }

        private static void ValidateCalendar(WorkingHoursCalendar calendar)
        {
            // no days
            if (calendar.Days.Count == 0)
            {
                throw new PlanarCalendarException($"calendar name'{calendar.CalendarName}' han no days");
            }

            // duplicate days
            var names = calendar.Days.Select(d => d.DayOfWeek).ToList();
            if (names.Count != names.Distinct().Count())
            {
                throw new PlanarCalendarException($"calendar name '{calendar.CalendarName}' has duplicated days");
            }

            // missing days
            Enum.GetNames(typeof(WorkingHoursDayType))
                .Where(n => n != nameof(WorkingHoursDayType.None))
                .ToList()
                .ForEach(n =>
                {
                    if (!calendar.Days.Exists(d => d.DayOfWeekMember.ToString() == n))
                    {
                        throw new PlanarCalendarException($"calendar name '{calendar.CalendarName}' has no day '{n}'");
                    }
                });

            // calendar name empty or not exists
            var exists =
                calendar.CalendarName == "default" ||
                CalendarInfo.Contains(calendar.CalendarName);

            if (!exists)
            {
                if (string.IsNullOrWhiteSpace(calendar.CalendarName))
                {
                    throw new PlanarCalendarException($"calendar name is null or empty");
                }
                else
                {
                    throw new PlanarCalendarException($"calendar name '{calendar}' is not supported");
                }
            }

            // validate days
            calendar.Days.ForEach(d => ValidateDay(d, calendar));
        }

        private static void ValidateDay(WorkingHoursDay day, WorkingHoursCalendar calendar)
        {
            // day of week ==> none
            if (day.DayOfWeekMember == WorkingHoursDayType.None)
            {
                throw new PlanarCalendarException($"day of week is none in calendar '{calendar.CalendarName}'");
            }

            // day of week empty or not exists
            if (day.DayOfWeekMember == WorkingHoursDayType.None)
            {
                if (string.IsNullOrWhiteSpace(day.DayOfWeek))
                {
                    throw new PlanarCalendarException($"day of week is null or empty in calendar '{calendar.CalendarName}'");
                }
                else
                {
                    throw new PlanarCalendarException($"day of week '{day.DayOfWeek}' in calendar '{calendar.CalendarName}' is not supported");
                }
            }

            // has default scopes but no default scopes at calendar
            if (day.DefaultScopes && !calendar.DefaultScopes.Any())
            {
                throw new PlanarCalendarException($"day of week '{day.DayOfWeek}' declare default scopes = true but calendar '{calendar.CalendarName}' has no default scopes");
            }

            // both scopes and default scopes
            if (day.DefaultScopes && day.Scopes.Any())
            {
                throw new PlanarCalendarException($"day of week '{day.DayOfWeek}' declare both default scopes = true and scopes. calendar '{calendar.CalendarName}'");
            }

            // validate scopes
            day.Scopes.ForEach(s => ValidateScope(s, day, calendar));
        }

        private static void ValidateScope(WorkingHourScope scope, WorkingHoursDay day, WorkingHoursCalendar calendar)
        {
            if (scope.Start.TotalDays >= 1)
            {
                throw new PlanarCalendarException($"scope start time '{scope.Start:\\(d\\)\\ hh\\:mm\\:ss}' is invalid in day of week '{day.DayOfWeek}' in calendar '{calendar.CalendarName}'");
            }

            if (scope.End.TotalDays >= 1)
            {
                throw new PlanarCalendarException($"scope end time '{scope.End:\\(d\\)\\ hh\\:mm\\:ss}' is invalid in day of week '{day.DayOfWeek}' in calendar '{calendar.CalendarName}'");
            }

            if (scope.Start >= scope.End)
            {
                throw new PlanarCalendarException($"scope start time '{scope.Start:hh\\:mm\\:ss}' is greater than or equal to end time '{scope.End:hh\\:mm\\:ss}' in day of week '{day.DayOfWeek}' in calendar '{calendar.CalendarName}'");
            }
        }
    }
}