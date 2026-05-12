using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Core.JsonConvertors;
using System.Text;
using System.Text.Json;

namespace Planar.Hooks.Serialize;

internal static class CoreSerializer
{
    private static readonly JsonSerializerOptions _jsonSerializerSettings = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new SystemTextTimeSpanConverter(),
            new SystemTextNullableTimeSpanConverter(),
        }
    };

    private static readonly JsonEventFormatter _formatter = new(
        serializerOptions: _jsonSerializerSettings,
         documentOptions: default
        );

    public static string Serialize(object obj)
    {
        return JsonSerializer.Serialize(obj, _jsonSerializerSettings);
    }

    public static string SerializeCloudEvent(CloudEvent cloudEvent)
    {
        var bytes = _formatter.EncodeStructuredModeMessage(cloudEvent, out _);
        var json = Encoding.UTF8.GetString(bytes.Span);
        return json;
    }
}