using CloudNative.CloudEvents;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Hook;
using Planar.Hooks.Serialize;
using StackExchange.Redis;
using System.Net.Mime;

namespace Planar.Hooks;

public sealed class PlanarRedisStreamHook(ILogger<PlanarRedisStreamHook> logger) : BaseSystemHook(logger)
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

    public override async Task Handle(IMonitorDetails monitorDetails)
    {
        await InvokePubSub(monitorDetails);
    }

    public override async Task HandleSystem(IMonitorSystemDetails monitorDetails)
    {
        await InvokePubSub(monitorDetails);
    }

    private static List<string> GetChannels(IMonitor monitor, RedisConnectorSettings settings)
    {
        var channels = new List<string>();

        foreach (var group in monitor.Groups)
        {
            var channel = GetParameter("redis-channel-name", group);

            if (!string.IsNullOrWhiteSpace(channel)) { channels.Add(channel); }
        }

        if (channels.Count == 0 && !string.IsNullOrWhiteSpace(settings.PubSubChannel))
        {
            channels.Add(settings.PubSubChannel);
        }

        if (channels.Count == 0)
        {
            return [];
        }

        return [.. channels.Distinct()];
    }

    private async Task InvokePubSub<T>(T details)
        where T : IMonitor
    {
        var settings = AppSettings.Connectors.Redis;
        if (settings == null)
        {
            LogError("Redis.PubSub.Hook: redis connector is not configured. set the configuration in appsettings.yml (Data folder of Planar)");
            return;
        }

        var channels = GetChannels(details, settings);
        if (channels.Count == 0)
        {
            LogError("Redis.PubSub.Hook: no pub sub channel(s)");
            return;
        }

        try
        {
#pragma warning disable S1075 // URIs should not be hardcoded
            var body = new CloudEvent
            {
                Id = Guid.NewGuid().ToString("N"),
                Time = DateTimeOffset.Now,
                Subject = Name,
                Data = details,
                DataContentType = MediaTypeNames.Application.Json,
                Source = new Uri("https://www.planar.me"),
                Type = typeof(T).Name
            };
#pragma warning restore S1075 // URIs should not be hardcoded

            body.SetAttributeFromString("version", "1.0.0");
            var json = CoreSerializer.SerializeCloudEvent(body);
            var connection = await RedisFactory.GetConnection(settings);
            var db = connection.GetDatabase(settings.Database);

            foreach (var channelName in channels)
            {
                var channel = new RedisChannel(channelName, RedisChannel.PatternMode.Auto);
                await db.PublishAsync(channel, json);
            }
        }
        catch (Exception ex)
        {
            throw new PlanarHookException($"fail to invoke '{details.MonitorTitle}' with '{Name}' hook. message: {ex.Message}", ex);
        }
    }
}