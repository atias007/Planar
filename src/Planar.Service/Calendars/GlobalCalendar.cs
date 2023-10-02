using Quartz.Impl.Calendar;
using System;

namespace Planar.Service.Calendars
{
    [Serializable]
    public class GlobalCalendar : BaseCalendar
    {
        public string? Name { get; set; }

        public string? Key { get; set; }

        public override bool IsTimeIncluded(DateTimeOffset timeStampUtc)
        {
            return true;
        }
    }
}