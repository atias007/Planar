using Planar.Common;
using StackExchange.Redis;

namespace Planar.Hooks
{
    internal static class RedisFactory
    {
        private static readonly object Locker = new();
        private static IConnectionMultiplexer _connection = null!;

        public static IConnectionMultiplexer Connection
        {
            get
            {
                if (_connection != null) { return _connection; }
                lock (Locker)
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
                        DefaultDatabase = AppSettings.Hooks.Redis.Database,
                        Ssl = AppSettings.Hooks.Redis.Ssl,
                    };

                    if (!string.IsNullOrWhiteSpace(AppSettings.Hooks.Redis.User))
                    {
                        options.User = AppSettings.Hooks.Redis.User;
                    }

                    if (!string.IsNullOrWhiteSpace(AppSettings.Hooks.Redis.Password))
                    {
                        options.Password = AppSettings.Hooks.Redis.Password;
                    }

                    if (!string.IsNullOrWhiteSpace(AppSettings.Hooks.Redis.ServiceName))
                    {
                        options.ServiceName = AppSettings.Hooks.Redis.ServiceName;
                    }

                    AppSettings.Hooks.Redis.Endpoints.ForEach(options.EndPoints.Add);
                    _connection = ConnectionMultiplexer.Connect(options);
                    return _connection;
                }
            }
        }
    }
}