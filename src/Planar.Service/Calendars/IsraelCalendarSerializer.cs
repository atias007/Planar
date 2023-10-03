using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;

namespace Planar.Service.Calendars
{
    public class IsraelCalendarSerializer : CalendarSerializer<IsraelCalendar>
    {
        protected override IsraelCalendar Create(JObject source)
        {
            return new IsraelCalendar();
        }

        protected override void DeserializeFields(IsraelCalendar calendar, JObject source)
        {
        }

        protected override void SerializeFields(JsonWriter writer, IsraelCalendar calendar)
        {
        }
    }
}