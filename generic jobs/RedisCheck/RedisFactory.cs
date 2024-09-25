using Common;
using Microsoft.Extensions.Configuration;
using Redis;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace RedisCheck;

internal static class RedisFactory
{
    private static readonly object Locker = new();
    private static IConnectionMultiplexer _connection = null!;
    private static readonly ConcurrentDictionary<int, IDatabase> _databases = new();

    private static int Database { get; set; }
    private static bool Ssl { get; set; }
    private static string? User { get; set; }
    private static string? Password { get; set; }
    private static IEnumerable<string> Endpoints { get; set; } = [];

    public static void Initialize(IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection("server");
        Database = section.GetValue<int?>("database") ?? 0;
        Ssl = section.GetValue<bool>("ssl");
        User = section.GetValue<string?>("user");
        Password = section.GetValue<string?>("password");
        Endpoints = section.GetSection("endpoints").Get<string[]>() ?? [];
    }

    public async static Task<TimeSpan> Ping()
    {
        return await Connection.GetDatabase().PingAsync();
    }

    public static async Task<long> GetMemoryUsage(IRedisKey redisKey)
    {
        var result = await GetDatabase(redisKey).ExecuteAsync("MEMORY", "USAGE", redisKey.Key);
        if (long.TryParse(result.ToString(), out var value))
        {
            return value;
        }

        return 0;
    }

    public static async Task<bool> SetExpire(IRedisKey redisKey, DateTime dateTime)
    {
        return await GetDatabase(redisKey).KeyExpireAsync(redisKey.Key, dateTime, ExpireWhen.HasNoExpiry);
    }

    public static async Task<bool> Exists(IRedisKey redisKey)
    {
        return await GetDatabase(redisKey).KeyExistsAsync(redisKey.Key);
    }

    public static async Task<string> Invoke(IRedisKey redisKey, string command, IEnumerable<string>? args = null)
    {
        var result =
            args == null ?
            await GetDatabase(redisKey).ExecuteAsync(command) :
            await GetDatabase(redisKey).ExecuteAsync(command, args.ToArray());

        return result.ToString();
    }

    public static async Task<IEnumerable<string>> Info(string section)
    {
        var db = Connection.GetDatabase();
        var value = await db.ExecuteAsync("INFO", section);
        return value.ToString().Split('\n').Select(l => l?.Trim() ?? string.Empty);
    }

    public static async Task<long> GetLength(IRedisKey redisKey)
    {
        var keyType = await Connection.GetDatabase().KeyTypeAsync(redisKey.Key);
        return keyType switch
        {
            RedisType.String => await GetDatabase(redisKey).StringLengthAsync(redisKey.Key),
            RedisType.List => await GetDatabase(redisKey).ListLengthAsync(redisKey.Key),
            RedisType.Set => await GetDatabase(redisKey).SetLengthAsync(redisKey.Key),
            RedisType.SortedSet => await GetDatabase(redisKey).SortedSetLengthAsync(redisKey.Key),
            RedisType.Hash => await GetDatabase(redisKey).HashLengthAsync(redisKey.Key),
            RedisType.Stream => await GetDatabase(redisKey).StreamLengthAsync(redisKey.Key),
            _ => 0,
        };
    }

    private static IDatabase GetDatabase(IRedisKey redisKey)
    {
        var database = redisKey.Database.GetValueOrDefault();
        return GetDatabase(database);
    }

    private static IDatabase GetDatabase(int database)
    {
        if (_databases.TryGetValue(database, out var db)) { return db; }
        lock (Locker)
        {
            if (_databases.TryGetValue(database, out db)) { return db; }
            db = Connection.GetDatabase(database);
            _databases.TryAdd(database, db);
            return db;
        }
    }

    private static IConnectionMultiplexer Connection
    {
        get
        {
            if (_connection != null) { return _connection; }
            lock (Locker)
            {
#pragma warning disable CA1508 // Avoid dead conditional code
                if (_connection != null) { return _connection; }
#pragma warning restore CA1508 // Avoid dead conditional code
                var options = new ConfigurationOptions
                {
                    ClientName = "Planar.Redis.StreamCheck",
                    ConfigCheckSeconds = 60,
                    ConnectRetry = 3,
                    ConnectTimeout = 10000,
                    DefaultDatabase = Database,
                    Ssl = Ssl
                };

                if (!string.IsNullOrWhiteSpace(User))
                {
                    options.User = User;
                }

                if (!string.IsNullOrWhiteSpace(Password))
                {
                    options.Password = Password;
                }

                Endpoints.ToList().ForEach(options.EndPoints.Add);
                _connection = ConnectionMultiplexer.Connect(options);
                return _connection;
            }
        }
    }
}