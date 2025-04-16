using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.Job;
using Redis;
using System.Text;

namespace RedisCheck;

internal partial class Job : BaseCheckJob
{
#pragma warning disable S3251 // Implementations should be provided for "partial" methods

    partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    partial void CustomConfigure(RedisServer redisServer, IConfiguration configuration);

    partial void VetoKey(RedisKey key);

    partial void Finalayze(FinalayzeDetails<IEnumerable<RedisKey>> details);

    partial void Finalayze(FinalayzeDetails<HealthCheck> details);

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
        CustomConfigure(configurationBuilder, context);

        var redisServer = new RedisServer();
        CustomConfigure(redisServer, configurationBuilder.Build());

        if (!redisServer.IsEmpty)
        {
            var json = JsonConvert.SerializeObject(new { server = redisServer });

            // Create a JSON stream as a MemoryStream or directly from a file
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Add the JSON stream to the configuration builder
            configurationBuilder.AddJsonStream(stream);
        }
    }

#pragma warning restore S3251 // Implementations should be provided for "partial" methods

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);
        RedisFactory.Initialize(Configuration);
        ValidateRedis();

        var defaults = GetDefaults(Configuration);
        var keys = GetKeys(context);
        var rediskeys = GetRedisKeys(Configuration, defaults, keys);
        var healthCheck = GetHealthCheck(Configuration, defaults);
        ValidateRequired(rediskeys, "keys");

        EffectedRows = 0;

        await SafeInvokeCheck(healthCheck, InvokeHealthCheckInner, context.TriggerDetails);
        await SafeInvokeCheck(rediskeys, InvokeKeyCheckInner, context.TriggerDetails);

        var hcDetails = GetFinalayzeDetails(healthCheck);
        Finalayze(hcDetails);
        var keysDetails = GetFinalayzeDetails(rediskeys.AsEnumerable());
        Finalayze(keysDetails);
        Finalayze();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterSpanCheck();
    }

    protected static void ValidateRedis()
    {
        ValidateRequired(RedisFactory.Endpoints, "endpoints", "server");
        ValidateGreaterThenOrEquals(RedisFactory.Database, 0, "database", "server");
        ValidateLessThenOrEquals(RedisFactory.Database, 16, "database", "server");
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
            result = new HealthCheck(hc, defaults);
        }

        ValidateHealthCheck(result);
        return result;
    }

    private List<RedisKey> GetRedisKeys(IConfiguration configuration, Defaults defaults)
    {
        var result = new List<RedisKey>();
        var rediskeys = configuration.GetRequiredSection("keys");
        foreach (var item in rediskeys.GetChildren())
        {
            var key = new RedisKey(item, defaults);

            VetoKey(key);
            if (CheckVeto(key, "key")) { continue; }

            ValidateRedisKey(key);
            result.Add(key);
        }

        return result;
    }

    private List<RedisKey> GetRedisKeys(IConfiguration configuration, Defaults defaults, IEnumerable<string>? keys)
    {
        if (keys == null || !keys.Any()) { return GetRedisKeys(configuration, defaults); }

        var result = new List<RedisKey>();
        var rediskeys = configuration.GetRequiredSection("keys");

        foreach (var item in rediskeys.GetChildren())
        {
            var key = new RedisKey(item, defaults);
            if (keys.Any(k => string.Equals(k, key.Key, StringComparison.OrdinalIgnoreCase)))
            {
                ValidateRedisKey(key);
                result.Add(key);
            }
        }

        return result;
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

        if (healthCheck.Ping.HasValue || healthCheck.Latency.HasValue)
        {
            TimeSpan span;
            try
            {
                span = await RedisFactory.Ping();
                healthCheck.ResultMessage = $"ping/latency health check ok. latency {span.TotalMilliseconds:N2}ms";
                Logger.LogInformation("ping/latency health check ok. latency {Latency:N2}ms", span.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                healthCheck.ResultMessage = $"ping/latency health check fail. reason: {ex.Message}";
                throw new CheckException($"ping/latency health check fail. reason: {ex.Message}");
            }

            if (healthCheck.Latency.HasValue && span.TotalMilliseconds > healthCheck.Latency.Value)
            {
                healthCheck.ResultMessage = $"latency of {span.TotalMilliseconds:N2} ms is greater then {healthCheck.Latency.Value:N0} ms";
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
                healthCheck.ResultMessage += $"\r\nconnected clients is {cc:N0}. maximum clients is {max:N0}".Trim();
                Logger.LogInformation("connected clients is {Clients:N0}. maximum clients is {MaxClients:N0}", cc, max);

                if (cc > healthCheck.ConnectedClients)
                {
                    healthCheck.ResultMessage = $"connected clients ({cc:N0}) is greater then {healthCheck.ConnectedClients:N0}";
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
                if (max > 0)
                {
                    healthCheck.ResultMessage += $"\r\nused memory is {memory:N0} bytes. maximum memory is {max:N0} bytes".Trim();
                    Logger.LogInformation("used memory is {Memory:N0} bytes. maximum memory is {MaxMemory:N0} bytes", memory, max);
                }
                else
                {
                    healthCheck.ResultMessage += $"\r\nused memory is {memory:N0} bytes".Trim();
                    Logger.LogInformation("used memory is {Memory:N0} bytes", memory);
                }
            }

            if (memory > healthCheck.UsedMemoryNumber)
            {
                healthCheck.ResultMessage = $"used memory ({memory:N0}) bytes is greater then {healthCheck.UsedMemoryNumber:N0} bytes";
                throw new CheckException($"used memory ({memory:N0}) bytes is greater then {healthCheck.UsedMemoryNumber:N0} bytes");
            }
        }

        IncreaseEffectedRows();
    }

    private async Task InvokeKeyCheckInner(RedisKey key)
    {
        var exists = await RedisFactory.Exists(key);
        key.Result.Exists = exists;
        if (key.Exists.GetValueOrDefault() && !exists)
        {
            throw new CheckException($"key '{key.Key}' is not exists");
        }

        long length = 0;
        long size = 0;
        if (key.Length > 0)
        {
            length = await RedisFactory.GetLength(key);
            key.Result.Length = length;
            key.ResultMessage = $"key '{key.Key}' length is {length:N0}";
            Logger.LogInformation("key '{Key}' length is {Length:N0}", key.Key, length);
        }

        if (key.MemoryUsageNumber > 0)
        {
            size = await RedisFactory.GetMemoryUsage(key);
            key.Result.MemoryUsage = size;
            key.ResultMessage += $"\r\nkey '{key.Key}' size is {size:N0} byte(s)".Trim();
            Logger.LogInformation("key '{Key}' size is {Size:N0} byte(s)", key.Key, size);
        }

        if (key.Length > 0 && length > key.Length)
        {
            key.ResultMessage = $"key '{key.Key}' length is greater then {key.Length:N0}";
            throw new CheckException($"key '{key.Key}' length is greater then {key.Length:N0}");
        }

        if (key.MemoryUsageNumber > 0 && size > key.MemoryUsageNumber)
        {
            key.ResultMessage = $"key '{key.Key}' size is greater then {key.MemoryUsage:N0}";
            throw new CheckException($"key '{key.Key}' size is greater then {key.MemoryUsage:N0}");
        }

        Logger.LogInformation("redis check success for key '{Key}'", key.Key);
        IncreaseEffectedRows();
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