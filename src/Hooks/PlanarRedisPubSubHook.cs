using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Planar.Common;
using Planar.Hook;
using Planar.Hooks.Enities;
using StackExchange.Redis;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Planar.Hooks;

public class PlanarRedisStreamHook : BaseSystemHook
{
    public override string Name => "Planar.Redis.PubSub";

    private static readonly JsonSerializerOptions _jsonSerializerSettings = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new SystemTextTimeSpanConverter()
        }
    };

    private static readonly JsonEventFormatter _formatter = new(
        serializerOptions: _jsonSerializerSettings,
         documentOptions: default
        );

    public override async Task Handle(IMonitorDetails monitorDetails)
    {
        await InvokeStream(monitorDetails);
    }

    public override async Task HandleSystem(IMonitorSystemDetails monitorDetails)
    {
        await InvokeStream(monitorDetails);
    }

    private async Task InvokeStream<T>(T detials)
        where T : IMonitor
    {
        if (string.IsNullOrWhiteSpace(AppSettings.Hooks.Redis.PubSubChannel))
        {
            LogError("Redis.Stream.Hook: pub sub channel is null or empty");
            return;
        }

        try
        {
            var body = new CloudEvent
            {
                Id = Guid.NewGuid().ToString("N"),
                Time = DateTimeOffset.Now,
                Subject = Name,
                Data = detials,
                DataContentType = MediaTypeNames.Application.Json,
                Source = new Uri("http://www.planar.com"),
                Type = typeof(T).Name
            };

            body.SetAttributeFromString("version", "1.0.0");
            var bytes = _formatter.EncodeStructuredModeMessage(body, out _);
            var json = Encoding.UTF8.GetString(bytes.Span);
            var db = RedisFactory.Connection.GetDatabase(AppSettings.Hooks.Redis.Database);
            var channel = new RedisChannel(AppSettings.Hooks.Redis.PubSubChannel, RedisChannel.PatternMode.Auto);
            await db.PublishAsync(channel, json);
        }
        catch (Exception ex)
        {
            throw new PlanarHookException($"fail to invoke '{detials.MonitorTitle}' with '{Name}' hook. message: {ex.Message}", ex);
        }
    }
}