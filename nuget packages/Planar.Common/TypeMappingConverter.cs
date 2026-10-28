using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Planar.Common
{
    internal class TypeMappingConverter<TType, TImplementation> : JsonConverter<TType>
        where TImplementation : TType
    {
#if NETSTANDARD2_0
#else
        [return: MaybeNull]
#endif

        public override TType Read(
          ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            JsonSerializer.Deserialize<TImplementation>(ref reader, options);

#if NETSTANDARD2_0

        public override void Write(
          Utf8JsonWriter writer, TType value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, (TImplementation)value, options);

#else
        public override void Write(
          Utf8JsonWriter writer, TType value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, (TImplementation)value!, options);
#endif
    }
}