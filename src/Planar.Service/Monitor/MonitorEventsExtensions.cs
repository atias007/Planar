using Planar.API.Common.Entities;

namespace Planar
{
    internal static class MonitorEventsExtensions
    {
        public static bool IsMonitorEventHasArguments(MonitorEvents @event)
        {
            var eventId = (int)@event;
            return IsMonitorEventHasArguments(eventId);
        }

        public static bool IsMonitorEventHasArguments(int eventId)
        {
            return eventId >= 200 && eventId < 300;
        }

        public static bool IsSystemMonitorEvent(MonitorEvents @event)
        {
            var eventId = (int)@event;
            return IsSystemMonitorEvent(eventId);
        }

        public static bool IsSystemMonitorEvent(int eventId)
        {
            return eventId >= 300 && eventId <= 400;
        }
    }
}