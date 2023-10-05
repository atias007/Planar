using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;

namespace Planar.Service.Calendars
{
    public class DefaultCalendarSerializer : CalendarSerializer<DefaultCalendar>
    {
        protected override DefaultCalendar Create(JObject source)
        {
            return new DefaultCalendar();
        }

        protected override void DeserializeFields(DefaultCalendar calendar, JObject source)
        {
        }

        protected override void SerializeFields(JsonWriter writer, DefaultCalendar calendar)
        {
        }
    }
}