using Core.JsonConvertors;
using Planar.Client.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Planar.Client
{
    internal static class CoreSerializer
    {
#if NETSTANDARD2_0
        private static JsonSerializerOptions _serOprions;

#else
        private static JsonSerializerOptions? _serOprions;
#endif
        private static readonly object _lock = new object();

        public static JsonSerializerOptions SerializerOptions
        {
            get
            {
                if (_serOprions != null) { return _serOprions; }
                lock (_lock)
                {
                    if (_serOprions != null) { return _serOprions; }

                    _serOprions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false,
                        AllowTrailingCommas = true,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                        Converters =
                        {
                            new SystemTextTimeSpanConverter(),
                            new SystemTextNullableTimeSpanConverter(),
                            new GenericEnumConverter<JobActiveMembers>(),
                            new GenericEnumConverter<Roles>(),
                            new GenericEnumConverter<ReportPeriods>()
                        }
                    };
                    return _serOprions;
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
        public static T? Deserialize<T>(string? json) where T: class
#endif
        {
            if (string.IsNullOrWhiteSpace(json)) { return null; }
            var entity = JsonSerializer.Deserialize<T>(json, SerializerOptions);
            return entity;
        }
    }
}