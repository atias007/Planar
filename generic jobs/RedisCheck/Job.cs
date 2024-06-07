using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace RedisCheck;

internal class Job : BaseCheckJob
{
    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);
        RedisFactory.Initialize(Configuration);

        var defaults = GetDefaults(Configuration);
        var keys = GetKeys(Configuration, defaults);
        var healthCheck = GetHealthCheck(Configuration, defaults);
        ValidateRequired(keys, "keys");
        ValidateDuplicateKeys(keys, "keys");

        var hcTask = SafeInvokeCheck(healthCheck, InvokeHealthCheckInner);
        var tasks = SafeInvokeCheck(keys, InvokeKeyCheckInner);
        await Task.WhenAll(tasks);
        await hcTask;

        CheckAggragateException();
        HandleCheckExceptions();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterBaseCheck();
    }

    private static void FillDefaults(RedisKey redisKey, Defaults defaults)
    {
        // Fill Defaults
        redisKey.Key ??= string.Empty;
        redisKey.Key = redisKey.Key.Trim();
        FillBase(redisKey, defaults);
        redisKey.Database ??= defaults.Database;
    }

    private static HealthCheck GetHealthCheck(IConfiguration configuration, Defaults defaults)
    {
        HealthCheck result;
        var hc = configuration.GetSection("health check");
        if (hc == null)
        {
            result = HealthCheck.Empty;
        }
        else
        {
            result = new HealthCheck(hc);
        }

        FillBase(result, defaults);
        ValidateHealthCheck(result);
        return result;
    }

    private static IEnumerable<RedisKey> GetKeys(IConfiguration configuration, Defaults defaults)
    {
        var keys = configuration.GetRequiredSection("keys");
        foreach (var item in keys.GetChildren())
        {
            var key = new RedisKey(item);
            FillDefaults(key, defaults);
            key.SetSize();
            ValidateRedisKey(key);
            yield return key;
        }
    }

    private static void Validate(IRedisDefaults redisKey, string section)
    {
        ValidateGreaterThenOrEquals(redisKey.Database, 0, "database", section);
        ValidateLessThenOrEquals(redisKey.Database, 16, "database", section);
    }

    private static void ValidateKey(RedisKey redisKey)
    {
        ValidateRequired(redisKey.Key, "key", "keys");
        ValidateMaxLength(redisKey.Key, 1024, "key", "keys");
    }

    private static void ValidateNoArguments(RedisKey redisKey)
    {
        if (!redisKey.IsValid)
        {
            throw new InvalidDataException($"key '{redisKey.Key}' has no arguments to check");
        }
    }

    private Defaults GetDefaults(IConfiguration configuration)
    {
        var section = GetDefaultSection(configuration, Logger);
        if (section == null)
        {
            return Defaults.Empty;
        }

        var result = new Defaults(section);
        Validate(result, "defaults");
        ValidateBase(result, "defaults");
        return result;
    }

    private async Task InvokeHealthCheckInner(HealthCheck healthCheck)
    {
        string? GetLineValue(IEnumerable<string> lines, string name)
        {
            if (lines == null) { return null; }
            var line = lines.FirstOrDefault(l => l.StartsWith($"{name}:"));
            if (string.IsNullOrWhiteSpace(line)) { return null; }
            return line[(name.Length + 1)..];
        }

        if (!healthCheck.Active)
        {
            Logger.LogInformation("skipping inactive health check");
            return;
        }

        if (healthCheck.Ping.HasValue || healthCheck.Latency.HasValue)
        {
            TimeSpan span;
            try
            {
                span = await RedisFactory.Ping();
                Logger.LogInformation("ping/latency health check ok. latency {Latency:N2}ms", span.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                throw new CheckException($"ping/latency health check fail. reason: {ex.Message}");
            }

            if (healthCheck.Latency.HasValue && span.TotalMilliseconds > healthCheck.Latency.Value)
            {
                throw new CheckException($"latency of {span.TotalMilliseconds:N2} ms is greater then {healthCheck.Latency.Value:N0} ms");
            }
        }

        if (healthCheck.ConnectedClients.HasValue)
        {
            var info = await RedisFactory.Info("Clients");
            var ccString = GetLineValue(info, "connected_clients");
            var maxString = GetLineValue(info, "maxclients");

            if (int.TryParse(ccString, out var cc) && int.TryParse(maxString, out var max))
            {
                Logger.LogInformation("connected clients is {Clients:N0}. maximum clients is {MaxClients:N0}", cc, max);

                if (cc > healthCheck.ConnectedClients)
                {
                    throw new CheckException($"connected clients ({cc:N0}) is greater then {healthCheck.ConnectedClients:N0}");
                }
            }
        }

        if (healthCheck.UsedMemoryNumber > 0)
        {
            var info = await RedisFactory.Info("Memory");
            var memString = GetLineValue(info, "used_memory");
            var maxString = GetLineValue(info, "maxmemory");

            if (int.TryParse(memString, out var memory) && int.TryParse(maxString, out var max))
            {
                Logger.LogInformation("used memory is {Memory:N0} bytes. maximum memory is {MaxMemory:N0} bytes", memory, max);
            }

            if (memory > healthCheck.UsedMemoryNumber)
            {
                throw new CheckException($"used memory ({memory:N0}) bytes is greater then {healthCheck.UsedMemoryNumber:N0} bytes");
            }
        }
    }

    private async Task InvokeKeyCheckInner(RedisKey key)
    {
        if (!key.Active)
        {
            Logger.LogInformation("skipping inactive key '{Key}'", key.Key);
            return;
        }

        if (!await RedisFactory.Exists(key))
        {
            throw new CheckException($"key '{key.Key}' is not exists");
        }

        long length = 0;
        long size = 0;
        if (key.Length > 0)
        {
            length = await RedisFactory.GetLength(key);
            Logger.LogInformation("key '{Key}' length is {Length:N0}", key.Key, length);
        }

        if (key.MemoryUsageNumber > 0)
        {
            size = await RedisFactory.GetMemoryUsage(key);
            Logger.LogInformation("key '{Key}' size is {Size:N0} byte(s)", key.Key, size);
        }

        if (key.Length > 0 && length > key.Length)
        {
            throw new CheckException($"key '{key.Key}' length is greater then {key.Length:N0}");
        }

        if (key.MemoryUsageNumber > 0 && size > key.MemoryUsageNumber)
        {
            throw new CheckException($"key '{key.Key}' size is greater then {key.MemoryUsage:N0}");
        }

        Logger.LogInformation("redis check success for key '{Key}'", key.Key);
    }

    private static void ValidateHealthCheck(HealthCheck healthCheck)
    {
        ValidateGreaterThen(healthCheck.ConnectedClients, 0, "connected clients", "health check");
        ValidateGreaterThen(healthCheck.UsedMemoryNumber, 0, "used memory", "health check");
    }

    private static void ValidateRedisKey(RedisKey redisKey)
    {
        ValidateBase(redisKey, $"key ({redisKey.Key})");
        Validate(redisKey, $"key ({redisKey.Key})");
        ValidateKey(redisKey);
        ValidateGreaterThen(redisKey.MemoryUsageNumber, 0, "max memory usage", "keys");
        ValidateGreaterThen(redisKey.Length, 0, "max length", "keys");
        ValidateNoArguments(redisKey);
    }
}