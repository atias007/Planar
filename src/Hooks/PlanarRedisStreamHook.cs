using Core.JsonConvertors;
using Planar.Common;
using Planar.Hook;
using StackExchange.Redis;
using System.Text.Json;

namespace Planar.Hooks;

public sealed class PlanarRedisPubSubHook : BaseSystemHook
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

    public override async Task Handle(IMonitorDetails monitorDetails)
    {
        await InvokeStream(monitorDetails);
    }

    public override async Task HandleSystem(IMonitorSystemDetails monitorDetails)
    {
        await InvokeStream(monitorDetails);
    }

    private List<string> GetStreamNames(IMonitor monitor)
    {
        if (string.IsNullOrWhiteSpace(AppSettings.Hooks.Redis.StreamName))
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
                stream = AppSettings.Hooks.Redis.PubSubChannel;
            }

            if (!string.IsNullOrWhiteSpace(stream)) { streams.Add(stream); }
        }

        return streams;
    }

    private async Task InvokeStream<T>(T detials)
        where T : IMonitor
    {
        var streams = GetStreamNames(detials);
        if (streams.Count == 0) { return; }

        try
        {
            var entries = new NameValueEntry[]
            {
                new("version", "1.0.0"),
                new("environment", detials.Environment),
                new("data-type", typeof(T).Name),
                new("data", JsonSerializer.Serialize(detials, _jsonSerializerSettings)),
                new("event-id", detials.EventId),
                new("group", detials.Groups.First().Name),
            };

            var db = RedisFactory.Connection.GetDatabase(AppSettings.Hooks.Redis.Database);

            foreach (var streamName in streams)
            {
                await db.StreamAddAsync(streamName, entries);
            }
        }
        catch (Exception ex)
        {
            throw new PlanarHookException($"fail to invoke '{detials.MonitorTitle}' with '{Name}' hook. message: {ex.Message}", ex);
        }
    }
}