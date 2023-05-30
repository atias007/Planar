using Newtonsoft.Json;

namespace Planar.Hooks
{
    public class NewtonsoftTimeSpanConverter : JsonConverter<TimeSpan>
    {
        private const string PathElement = "ticks";

        public override void WriteJson(JsonWriter writer, TimeSpan value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(PathElement);
            writer.WriteValue(value.Ticks);
            writer.WriteEndObject();
        }

        public override TimeSpan ReadJson(JsonReader reader, Type objectType, TimeSpan existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                return FromObject(reader, serializer);
            }

            if (reader.TokenType == JsonToken.String)
            {
                return FromString(reader, serializer);
            }

            throw new JsonSerializationException($"Fail to deselrialize TimeSpan. Token type '{reader.TokenType}' is not expected");
        }

        private static TimeSpan FromString(JsonReader reader, JsonSerializer serializer)
        {
            var value = reader.Value;
            var ts = Convert.ToString(value, serializer.Culture);
            if (ts == null) { return TimeSpan.Zero; }
            var result = TimeSpan.Parse(ts, serializer.Culture);
            return result;
        }

        private static TimeSpan FromObject(JsonReader reader, JsonSerializer serializer)
        {
            reader.Read();
            var jsonpath = Convert.ToString(reader.Value, serializer.Culture);
            if (!string.Equals(jsonpath, PathElement, StringComparison.OrdinalIgnoreCase))
            {
                throw new JsonSerializationException($"Fail to deselrialize TimeSpan. Token '{jsonpath}' is not expected");
            }

            reader.Read();
            try
            {
                var value = reader.Value;
                var ticks = Convert.ToInt64(value, serializer.Culture);
                var result = TimeSpan.FromTicks(ticks);
                return result;
            }
            finally
            {
                reader.Read();
            }
        }
    }
}