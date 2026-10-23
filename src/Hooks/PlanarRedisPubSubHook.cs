using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Hook;
using Planar.Hooks.Serialize;
using StackExchange.Redis;

namespace Planar.Hooks;

public sealed class PlanarRedisPubSubHook(ILogger<PlanarRedisPubSubHook> logger) : BaseSystemHook(logger)
{
    public override string Name => "Planar.Redis.Stream";

    public override string Description =>
"""
This hook use redis server to add message to stream component.
You can find the configuration of redis server is in appsettings.yml (Data folder of Planar).
The configuration also define the default stream name.
To use different stream name per group, you can set one of the 'AdditionalField' of monitor group to the following value:
--------------------------------------
  redis-stream-name:<your-stream-name>
--------------------------------------
""";

    public override async Task Handle(IMonitorDetails monitorDetails)
    {
        var settings = AppSettings.Connectors.Redis;
        if (settings == null)
        {
            LogError("Redis.Stream.Hook: Redis connector is not configured. set the configuration in appsettings.yml (Data folder of Planar)");
            return;
        }

        await InvokeStream(monitorDetails, settings);
    }

    public override async Task HandleSystem(IMonitorSystemDetails monitorDetails)
    {
        var settings = AppSettings.Connectors.Redis;
        if (settings == null)
        {
            LogError("Redis.Stream.Hook: Redis connector is not configured. set the configuration in appsettings.yml (Data folder of Planar)");
            return;
        }

        await InvokeStream(monitorDetails, settings);
    }

    private IEnumerable<string> GetStreamNames(IMonitor monitor, RedisConnectorSettings settings)
    {
        var streamName = settings.StreamName;
        if (string.IsNullOrWhiteSpace(streamName))
        {
            LogError("Redis.Stream.Hook: stream name is null or empty");
            return [];
        }

        var streams = new List<string>();
        foreach (var group in monitor.Groups)
        {
            var stream = GetParameter("redis-stream-name", group);
            if (string.IsNullOrWhiteSpace(stream))
            {
                stream = streamName;
            }

            if (!string.IsNullOrWhiteSpace(stream)) { streams.Add(stream); }
        }

        return streams.Distinct();
    }

    private async Task InvokeStream<T>(T details, RedisConnectorSettings settings)
        where T : IMonitor
    {
        var streams = GetStreamNames(details, settings);
        if (!streams.Any()) { return; }

        try
        {
            var entries = new NameValueEntry[]
            {
                new("version", "1.0.0"),
                new("environment", details.Environment),
                new("data-type", typeof(T).Name),
                new("data", CoreSerializer.Serialize(details)),
                new("event-id", details.EventId),
                new("group", details.Groups.First().Name),
            };

            var connection = await RedisFactory.GetConnection(settings);
            var db = connection.GetDatabase(settings.Database);

            foreach (var streamName in streams)
            {
                await CreateRedisStreamIfNotExists(db, streamName, settings);
                await db.StreamAddAsync(streamName, entries);
            }
        }
        catch (Exception ex)
        {
            throw new PlanarHookException($"fail to invoke '{details.MonitorTitle}' with '{Name}' hook. message: {ex.Message}", ex);
        }
    }

    private static async Task CreateRedisStreamIfNotExists(IDatabase db, string streamName, RedisConnectorSettings settings)
    {
        var exists = await db.KeyExistsAsync(streamName);
        if (!exists)
        {
            await db.StreamAddAsync(streamName, [new("version", "1.0.0")], maxLength: settings.StreamSize, useApproximateMaxLength: true);

            // clear all stream item
            var entries = await db.StreamRangeAsync(streamName);
            var ids = entries.Select(e => e.Id).ToArray();
            await db.StreamDeleteAsync(streamName, ids);
        }
    }
}