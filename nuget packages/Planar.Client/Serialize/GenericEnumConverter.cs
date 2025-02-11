using Newtonsoft.Json;
using System;

namespace Planar.Client.Serialize
{
    internal class GenericEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                if (reader.TokenType == JsonToken.Integer)
                {
                    if (reader.Value == null) { return default; }
                    var value = Convert.ToInt64(reader.Value);
                    return (T)Enum.ToObject(typeof(T), value);
                }
                else if (reader.TokenType == JsonToken.String)
                {
#if NETSTANDARD2_0
                    var value = (string)reader.Value;
                    if (string.IsNullOrWhiteSpace(value)) { return default; }
                    return (T)Enum.Parse(typeof(T), value, ignoreCase: true);
#else
                    var value = (string?)reader.Value;
                    if (string.IsNullOrWhiteSpace(value)) { return default; }
                    return Enum.Parse<T>(value, ignoreCase: true);
#endif
                }

                throw new JsonSerializationException($"Unexpected token when parsing enum: {reader.TokenType}");
            }
            catch (Exception)
            {
                throw new JsonException($"Unexpected JSON token when parsing enum {typeof(T).Name}");
            }
        }

        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
        {
            writer.WriteValue((int)(object)value);
        }
    }
}