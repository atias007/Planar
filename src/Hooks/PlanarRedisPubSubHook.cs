using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Core.JsonConvertors;
using Planar.Common;
using Planar.Hook;
using StackExchange.Redis;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Planar.Hooks;

public sealed class PlanarRedisStreamHook : BaseSystemHook
{
    public override string Name => "Planar.Redis.PubSub";

    public override string Description =>
"""
This hook use redis server to publish message via Pub/Sub component.
You can find the configuration of redis server is in appsettings.yml (Data folder of Planar).
The configuration also define the default channel name.
To use different channel name, you can set one of the 'AdditionalField' of monitor group to the following value:
----------------------------------------
  redis-channel-name:<your-channel-name>
----------------------------------------
""";

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

    public override async Task Handle(IMonitorDetails monitorDetails)
    {
        await InvokePubSub(monitorDetails);
    }

    public override async Task HandleSystem(IMonitorSystemDetails monitorDetails)
    {
        await InvokePubSub(monitorDetails);
    }

    private List<string> GetChannels(IMonitor monitor)
    {
        if (string.IsNullOrWhiteSpace(AppSettings.Hooks.Redis.PubSubChannel))
        {
            LogError("Redis.Stream.Hook: pub sub channel is null or empty");
            return [];
        }

        var channels = new List<string>();
        foreach (var group in monitor.Groups)
        {
            var channel = GetParameter("redis-channel-name", group);
            if (string.IsNullOrWhiteSpace(channel))
            {
                channel = AppSettings.Hooks.Redis.PubSubChannel;
            }

            if (!string.IsNullOrWhiteSpace(channel)) { channels.Add(channel); }
        }

        return channels;
    }

    private async Task InvokePubSub<T>(T detials)
        where T : IMonitor
    {
        var channels = GetChannels(detials);
        if (channels.Count == 0) { return; }

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

            foreach (var channelName in channels)
            {
                var channel = new RedisChannel(channelName, RedisChannel.PatternMode.Auto);
                await db.PublishAsync(channel, json);
            }
        }
        catch (Exception ex)
        {
            throw new PlanarHookException($"fail to invoke '{detials.MonitorTitle}' with '{Name}' hook. message: {ex.Message}", ex);
        }
    }
}