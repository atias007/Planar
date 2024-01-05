using Planar.Common;
using Planar.Hook;
using Planar.Hooks.Enities;
using StackExchange.Redis;
using System.Text.Json;

namespace Planar.Hooks;

public class PlanarRedisPubSubHook : BaseSystemHook
{
    public override string Name => "Planar.Redis.Stream";

    public override string Description =>
"""
This hook use redis server to add message to stream component.
You can find the configuration of redis server is in appsettings.yml (data folder of Planar).
The configuration also define the default stream name.
To use different stream name, you can set one of the 'AdditionalField' of monitor group to the following value:
  redis-stream-name:your-stream-name
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
        if (string.IsNullOrWhiteSpace(AppSettings.Hooks.Redis.StreamName))
        {
            LogError("Redis.Stream.Hook: stream name is null or empty");
            return;
        }

        try
        {
            var entries = new NameValueEntry[]
            {
                new NameValueEntry("version", "1.0.0"),
                new NameValueEntry("environment", detials.Environment),
                new NameValueEntry("data-type", typeof(T).Name),
                new NameValueEntry("data", JsonSerializer.Serialize(detials, _jsonSerializerSettings)),
                new NameValueEntry("event-id", detials.EventId),
                new NameValueEntry("group", detials.Group.Name),
            };

            var db = RedisFactory.Connection.GetDatabase(AppSettings.Hooks.Redis.Database);
            await db.StreamAddAsync(AppSettings.Hooks.Redis.StreamName, entries);
        }
        catch (Exception ex)
        {
            throw new PlanarHookException($"fail to invoke '{detials.MonitorTitle}' with '{Name}' hook. message: {ex.Message}", ex);
        }
    }
}