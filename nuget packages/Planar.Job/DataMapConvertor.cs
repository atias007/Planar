using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Planar.Job
{
    internal class DataMapConvertor : JsonConverter<IDataMap>
    {
        public override IDataMap? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dic = JsonSerializer.Deserialize<Dictionary<string, string?>>(ref reader, options);
            return new DataMap(dic);
        }

        public override void Write(Utf8JsonWriter writer, IDataMap value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}