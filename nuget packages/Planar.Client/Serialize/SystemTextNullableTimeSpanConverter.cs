using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.JsonConvertors
{
    internal class SystemTextNullableTimeSpanConverter : JsonConverter<TimeSpan?>
    {
        private const string PathElement = "ticks";

        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                return FromObject(ref reader);
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                return FromString(ref reader);
            }

            throw new JsonException($"Fail to deselrialize TimeSpan. Token type '{reader.TokenType}' is not expected");
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(PathElement);
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteNumberValue(value.Value.Ticks);
            }
            writer.WriteEndObject();
        }

        private static TimeSpan? FromString(ref Utf8JsonReader reader)
        {
            var value = reader.GetString();
            if (value == null) { return default; }
            var result = TimeSpan.Parse(value, CultureInfo.CurrentCulture);
            return result;
        }

        private static TimeSpan? FromObject(ref Utf8JsonReader reader)
        {
            reader.Read();
            var jsonpath = reader.GetString();
            if (!string.Equals(jsonpath, PathElement, StringComparison.OrdinalIgnoreCase))
            {
                throw new JsonException($"Fail to deselrialize TimeSpan. Token '{jsonpath}' is not expected");
            }

            reader.Read();
            try
            {
                var value = reader.GetInt64();
                var result = TimeSpan.FromTicks(value);
                return result;
            }
            finally
            {
                reader.Read();
            }
        }
    }
}