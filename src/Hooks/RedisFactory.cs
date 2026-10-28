using Planar.Common;
using StackExchange.Redis;

namespace Planar.Hooks;

internal static class RedisFactory
{
    private static readonly SemaphoreSlim Locker = new(1, 1);
    private static IConnectionMultiplexer _connection = null!;

    public static async Task<IConnectionMultiplexer> GetConnection(RedisConnectorSettings settings)
    {
        if (_connection != null) { return _connection; }
        await Locker.WaitAsync();
        try
        {
            if (_connection != null) { return _connection; }
            var options = new ConfigurationOptions
            {
                // --- Diagnostics ---
                ClientName = "Planar.Redis.Hook",             // shows up in CLIENT LIST / SLOWLOG context
                IncludeDetailInExceptions = true,             // includes server endpoint, command, etc.
                IncludePerformanceCountersInExceptions = true,// includes ThreadPool/IOCP stats in timeout messages

                // --- The single most important one ---
                AbortOnConnectFail = false,   // don't throw at startup if Redis is down; keep retrying in background

                // --- Connect-phase ---
                ConnectRetry = 3,           // attempts during initial connect
                ConnectTimeout = 10_000,        // ms; lower (2000-3000) if you prefer fast-fail

                // --- Operation timeouts (these surface as RedisTimeoutException) ---
                SyncTimeout = 5000,          // ms
                AsyncTimeout = 5000,          // ms

                // --- Liveness ---
                KeepAlive = 60,               // s; PING interval to detect dead sockets

                // --- Reconnect behavior ---
                ReconnectRetryPolicy = new ExponentialRetry(5000), // backoff cap-ish, gentler on a recovering server

                ConfigCheckSeconds = 60,
                DefaultDatabase = settings.Database,
                Ssl = settings.Ssl,
            };

            if (!string.IsNullOrWhiteSpace(settings.User))
            {
                options.User = settings.User;
            }

            if (!string.IsNullOrWhiteSpace(settings.Password))
            {
                options.Password = settings.Password;
            }

            if (!string.IsNullOrWhiteSpace(settings.ServiceName))
            {
                options.ServiceName = settings.ServiceName;
            }

            foreach (var ep in settings.Endpoints)
            {
                options.EndPoints.Add(ep);
            }
            _connection = await ConnectionMultiplexer.ConnectAsync(options);
            return _connection;
        }
        finally
        {
            Locker.Release();
        }
    }
}