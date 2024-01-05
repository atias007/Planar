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
                        ClientName = "Planar.Redis.Hook",
                        ConfigCheckSeconds = 60,
                        ConnectRetry = 3,
                        ConnectTimeout = 10000,
                        DefaultDatabase = AppSettings.Hooks.Redis.Database,
                        Ssl = AppSettings.Hooks.Redis.Ssl
                    };

                    if (!string.IsNullOrWhiteSpace(AppSettings.Hooks.Redis.User))
                    {
                        options.User = AppSettings.Hooks.Redis.User;
                    }

                    if (!string.IsNullOrWhiteSpace(AppSettings.Hooks.Redis.Password))
                    {
                        options.Password = AppSettings.Hooks.Redis.Password;
                    }

                    AppSettings.Hooks.Redis.Endpoints.ForEach(options.EndPoints.Add);
                    _connection = ConnectionMultiplexer.Connect(options);
                    return _connection;
                }
            }
        }
    }
}