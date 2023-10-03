using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;

namespace Planar.Service.Calendars
{
    public class GlobalCalendarSerializer : CalendarSerializer<GlobalCalendar>
    {
        protected override GlobalCalendar Create(JObject source)
        {
            var result = source.ToObject<GlobalCalendar>()!;
            return result;
        }

        protected override void DeserializeFields(GlobalCalendar calendar, JObject source)
        {
        }

        protected override void SerializeFields(JsonWriter writer, GlobalCalendar calendar)
        {
        }
    }
}