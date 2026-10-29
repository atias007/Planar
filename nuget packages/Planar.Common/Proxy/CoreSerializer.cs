using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Planar.Common.Proxy
{
    internal static class CoreSerializer
    {
#if NETSTANDARD2_0
        private static JsonSerializerOptions _serOptions;

#else
        private static JsonSerializerOptions? _serOptions;
#endif
        private static readonly object _lock = new object();

        public static JsonSerializerOptions SerializerOptions
        {
            get
            {
                if (_serOptions != null) { return _serOptions; }
                lock (_lock)
                {
                    if (_serOptions != null) { return _serOptions; }

                    _serOptions = new JsonSerializerOptions
                    {
                        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false,
                        AllowTrailingCommas = true,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    };
                    return _serOptions;
                }
            }
        }

#if NETSTANDARD2_0

        public static string Serialize(object obj)

#else
        public static string? Serialize(object? obj)
#endif
        {
            if (obj == null) { return null; }
            var json = JsonSerializer.Serialize(obj, SerializerOptions);
            return json;
        }

#if NETSTANDARD2_0

        public static T Deserialize<T>(string json) where T : class

#else
        public static T? Deserialize<T>(string? json) where T : class
#endif
        {
            if (string.IsNullOrWhiteSpace(json)) { return null; }
            var entity = JsonSerializer.Deserialize<T>(json, SerializerOptions);
            return entity;
        }
    }
}