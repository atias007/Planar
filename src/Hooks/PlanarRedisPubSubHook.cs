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

    public override string Description =>
"""
This hook use redis server to publish message via Pub/Sub component.
You can find the configuration of redis server is in appsettings.yml (Data folder of Planar).
The configuration also define the default channel name.
To use different channel name, you can set one of the 'AdditionalField' of monitor group to the following value:
  redis-channel-name:your-channel-name
""";

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

    private string? GetChannel(IMonitor monitor)
    {
        var channel = GetParameter("redis-channel-name", monitor.Group);
        if (string.IsNullOrWhiteSpace(channel))
        {
            channel = AppSettings.Hooks.Redis.PubSubChannel;
        }

        if (string.IsNullOrWhiteSpace(AppSettings.Hooks.Redis.PubSubChannel))
        {
            LogError("Redis.Stream.Hook: pub sub channel is null or empty");
            return null;
        }

        return channel;
    }

    private async Task InvokeStream<T>(T detials)
        where T : IMonitor
    {
        var channelName = GetChannel(detials);
        if (string.IsNullOrWhiteSpace(channelName)) { return; }

        try
        {
#pragma warning disable S1075 // URIs should not be hardcoded
            var body = new CloudEvent
            {
                Id = Guid.NewGuid().ToString("N"),
                Time = DateTimeOffset.Now,
                Subject = Name,
                Data = detials,
                DataContentType = MediaTypeNames.Application.Json,
                Source = new Uri("https://www.planar.me"),
                Type = typeof(T).Name
            };
#pragma warning restore S1075 // URIs should not be hardcoded

            body.SetAttributeFromString("version", "1.0.0");
            var bytes = _formatter.EncodeStructuredModeMessage(body, out _);
            var json = Encoding.UTF8.GetString(bytes.Span);
            var db = RedisFactory.Connection.GetDatabase(AppSettings.Hooks.Redis.Database);
            var channel = new RedisChannel(channelName, RedisChannel.PatternMode.Auto);
            await db.PublishAsync(channel, json);
        }
        catch (Exception ex)
        {
            throw new PlanarHookException($"fail to invoke '{detials.MonitorTitle}' with '{Name}' hook. message: {ex.Message}", ex);
        }
    }
}