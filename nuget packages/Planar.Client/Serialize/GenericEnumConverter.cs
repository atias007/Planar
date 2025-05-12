using System.Text.Json;
using System;
using System.Text.Json.Serialization;

namespace Core.JsonConvertors
{
    internal class GenericEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                if (reader.TokenType == JsonTokenType.Null) { return default; }
                if (reader.TokenType == JsonTokenType.Number)
                {
                    if (!reader.TryGetUInt32(out var value)) { throw new JsonException($"Invalid enum value {reader.GetString()}"); }
                    return (T)Enum.ToObject(typeof(T), value);
                }
                else if (reader.TokenType == JsonTokenType.String)
                {
#if NETSTANDARD2_0
                    var value = reader.GetString();
                    if (string.IsNullOrWhiteSpace(value)) { return default; }
                    return (T)Enum.Parse(typeof(T), value, ignoreCase: true);
#else
                    var value = reader.GetString();
                    if (string.IsNullOrWhiteSpace(value)) { return default; }
                    return Enum.Parse<T>(value, ignoreCase: true);
#endif
                }

                throw new JsonException($"Unexpected token when parsing enum: {reader.TokenType}");
            }
            catch (Exception)
            {
                throw new JsonException($"Unexpected JSON token when parsing enum {typeof(T).Name}");
            }
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var iValue = (int)(object)value;
            writer.WriteNumberValue(iValue);
        }
    }
}