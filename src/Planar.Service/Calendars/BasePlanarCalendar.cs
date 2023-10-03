using Microsoft.Extensions.Logging;
using Quartz.Impl.Calendar;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Planar.Service.Calendars
{
    public abstract class BasePlanarCalendar : BaseCalendar, ICalendarWithLogger
    {
        private sealed record LogEntry(LogLevel LogLevel, Exception? Exception, string? Message, params object?[] Args);

        private readonly ConcurrentQueue<LogEntry> LogEntries = new();

        public ILogger? Logger { get; set; }

        protected WorkingHoursCalendar WorkingHours { get; set; } = null!;

        protected void Log(LogLevel logLevel, string? message, params object?[] args)
        {
            if (Logger == null)
            {
                LogEntries.Enqueue(new LogEntry(logLevel, null, message, args));
            }
            else
            {
                PurgeQueue();
                Logger.Log(logLevel, message, args);
            }
        }

        protected void Log(LogLevel logLevel, Exception? exception, string? message, params object?[] args)
        {
            if (Logger == null)
            {
                LogEntries.Enqueue(new LogEntry(logLevel, exception, message, args));
            }
            else
            {
                PurgeQueue();
                Logger.Log(logLevel, exception, message, args);
            }
        }

        protected static WorkingHoursDayType Convert(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Sunday => WorkingHoursDayType.Sunday,
                DayOfWeek.Monday => WorkingHoursDayType.Monday,
                DayOfWeek.Tuesday => WorkingHoursDayType.Tuesday,
                DayOfWeek.Wednesday => WorkingHoursDayType.Wednesday,
                DayOfWeek.Thursday => WorkingHoursDayType.Thursday,
                DayOfWeek.Friday => WorkingHoursDayType.Friday,
                DayOfWeek.Saturday => WorkingHoursDayType.Saturday,
                _ => throw new PlanarCalendarException($"Invalid day of week '{dayOfWeek}'"),
            };
        }

        protected bool IsWorkingDateTime(WorkingHoursDayType dayType, DateTime date)
        {
            var day = WorkingHours.WorkingHourDay(dayType);
            if (day == null) { return false; }

            var scopes = day.DefaultScopes ? WorkingHours.DefaultScopes : day.Scopes;
            if (scopes == null || !scopes.Any()) { return false; }

            foreach (var s in scopes)
            {
                if (s.IsTimeIncluded(date)) { return true; }
            }

            return false;
        }

        private void PurgeQueue()
        {
            if (Logger == null) { return; }

            while (!LogEntries.IsEmpty)
            {
                if (LogEntries.TryDequeue(out var logEntry))
                {
                    Logger.Log(logEntry.LogLevel, logEntry.Exception, logEntry.Message, logEntry.Args);
                }
            }
        }
    }
}