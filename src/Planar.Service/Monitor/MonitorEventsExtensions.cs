using Planar.Common;
using System;

namespace Planar
{
    internal static class MonitorEventsExtensions
    {
        public static bool IsSimpleJobMonitorEvent(string? eventName)
        {
            if (!Enum.TryParse(eventName, out MonitorEvents @event)) { return false; }
            return IsSimpleJobMonitorEvent(@event);
        }

        public static bool IsSimpleJobMonitorEvent(MonitorEvents @event)
        {
            var eventId = (int)@event;
            return IsSimpleJobMonitorEvent(eventId);
        }

        public static bool IsSimpleJobMonitorEvent(int eventId)
        {
            return eventId >= 100 && eventId < 200;
        }

        // -------------------------

        public static bool IsMonitorEventHasArguments(string? eventName)
        {
            if (!Enum.TryParse(eventName, out MonitorEvents @event)) { return false; }
            return IsMonitorEventHasArguments(@event);
        }

        public static bool IsMonitorEventHasArguments(MonitorEvents @event)
        {
            var eventId = (int)@event;
            return IsMonitorEventHasArguments(eventId);
        }

        public static bool IsMonitorEventHasArguments(int eventId)
        {
            return eventId >= 200 && eventId < 300;
        }

        // -------------------------

        public static bool IsSystemMonitorEvent(string? eventName)
        {
            if (!Enum.TryParse(eventName, out MonitorEvents @event)) { return false; }
            return IsSystemMonitorEvent(@event);
        }

        public static bool IsSystemMonitorEvent(MonitorEvents @event)
        {
            var eventId = (int)@event;
            return IsSystemMonitorEvent(eventId);
        }

        public static bool IsSystemMonitorEvent(int eventId)
        {
            return eventId >= 300 && eventId < 400;
        }

        public static bool IsCustomMonitorEvent(MonitorEvents @event)
        {
            var eventId = (int)@event;
            return IsCustomMonitorEvent(eventId);
        }

        public static bool IsCustomMonitorEvent(int eventId)
        {
            return eventId >= 400 && eventId < 500;
        }
    }
}