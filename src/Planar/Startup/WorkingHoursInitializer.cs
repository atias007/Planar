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
                c.Calendar = c.Calendar?.ToLower();
                c.DefaultScopes ??= new List<WorkingHourScope>();
                c.Days ??= new List<WorkingHoursDay>();
                c.Days.ForEach(d =>
                {
                    d.DayOfWeek = d.DayOfWeek?.ToLower();
                    d.Scopes ??= new List<WorkingHourScope>();
                });
            });
        }

        private static void Validate()
        {
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
                throw new PlanarCalendarException($"Calendar '{calendar.Calendar}' han no days");
            }

            // calendar empty or not exists
            var exists =
                calendar.Calendar == "default" ||
                CalendarInfo.Contains(calendar.Calendar);

            if (!exists)
            {
                if (string.IsNullOrWhiteSpace(calendar.Calendar))
                {
                    throw new PlanarCalendarException($"Calendar is null or empty");
                }
                else
                {
                    throw new PlanarCalendarException($"Calendar '{calendar}' is not supported");
                }
            }

            // validate days
            calendar.Days.ForEach(d => ValidateDay(d, calendar));
        }

        private static void ValidateDay(WorkingHoursDay day, WorkingHoursCalendar calendar)
        {
            // day of week empty or not exists
            if (day.DayOfWeekMember == WorkingHoursDayType.None)
            {
                if (string.IsNullOrWhiteSpace(day.DayOfWeek))
                {
                    throw new PlanarCalendarException($"Day of week is null or empty in calendar '{calendar.Calendar}'");
                }
                else
                {
                    throw new PlanarCalendarException($"Day of week '{day.DayOfWeek}' in calendar '{calendar.Calendar}' is not supported");
                }
            }

            // has default scopes but no default scopes at calendar
            if (day.DefaultScopes && !calendar.DefaultScopes.Any())
            {
                throw new PlanarCalendarException($"Day of week '{day.DayOfWeek}' declare default scopes = true but calendar '{calendar.Calendar}' has no default scopes");
            }

            // both scopes and default scopes
            if (day.DefaultScopes && day.Scopes.Any())
            {
                throw new PlanarCalendarException($"Day of week '{day.DayOfWeek}' declare both default scopes = true and scopes. calendar '{calendar.Calendar}'");
            }

            // validate scopes
            day.Scopes.ForEach(s => ValidateScope(s, day, calendar));
        }

        private static void ValidateScope(WorkingHourScope scope, WorkingHoursDay day, WorkingHoursCalendar calendar)
        {
            if (scope.Start.TotalDays >= 1)
            {
                throw new PlanarCalendarException($"Scope start time '{scope.Start:\\(d\\)\\ hh\\:mm\\:ss}' is invalid in day of week '{day.DayOfWeek}' in calendar '{calendar.Calendar}'");
            }

            if (scope.End.TotalDays >= 1)
            {
                throw new PlanarCalendarException($"Scope end time '{scope.End:\\(d\\)\\ hh\\:mm\\:ss}' is invalid in day of week '{day.DayOfWeek}' in calendar '{calendar.Calendar}'");
            }

            if (scope.Start >= scope.End)
            {
                throw new PlanarCalendarException($"Scope start time '{scope.Start:hh\\:mm\\:ss}' is greater than or equal to end time '{scope.End:hh\\:mm\\:ss}' in day of week '{day.DayOfWeek}' in calendar '{calendar.Calendar}'");
            }
        }
    }
}