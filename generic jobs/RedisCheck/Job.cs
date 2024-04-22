using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using RedisCheck;
using RedisStream;

namespace RedisStreamCheck;

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
        var keys = GetKeys(Configuration);
        ValidateKeys(keys);
        CheckAggragateException();
        FillDefaults(keys, defaults);
        var tasks = SafeInvokeCheck(keys, InvokeKeyCheckInner);
        await Task.WhenAll(tasks);
        CheckAggragateException();
        HandleCheckExceptions();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterBaseCheck();
    }

    private static void FillDefaults(IEnumerable<CheckKey> redisKeys, Defaults defaults)
    {
        foreach (var f in redisKeys)
        {
            FillDefaults(f, defaults);
        }
    }

    private static void FillDefaults(CheckKey redisKey, Defaults defaults)
    {
        // Fill Defaults
        redisKey.Key ??= string.Empty;
        redisKey.Key = redisKey.Key.Trim();
        FillBase(redisKey, defaults);
        SetDefault(redisKey, () => defaults.Database);
    }

    private static IEnumerable<CheckKey> GetKeys(IConfiguration configuration)
    {
        var keys = configuration.GetRequiredSection("keys");
        foreach (var item in keys.GetChildren())
        {
            var folder = new CheckKey(item);
            yield return folder;
        }
    }

    private static void Validate(IRedisDefaults redisKey, string section)
    {
        ValidateGreaterThenOrEquals(redisKey.Database, 0, "database", section);
        ValidateLessThenOrEquals(redisKey.Database, 16, "database", section);
    }

    private static void ValidateKey(CheckKey redisKey)
    {
        ValidateRequired(redisKey.Key, "key", "keys");
        ValidateMaxLength(redisKey.Key, 1024, "key", "keys");
    }

    private static void ValidateNoArguments(CheckKey redisKey)
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
        ValidateBase(result, "defaults");
        Validate(result, "defaults");
        return result;
    }

    private async Task InvokeKeyCheckInner(CheckKey key)
    {
        if (!ValidateRedisKey(key)) { return; }

        await RedisFactory.Exists(key);

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

    private void ValidateKeys(IEnumerable<CheckKey> keys)
    {
        try
        {
            ValidateRequired(keys, "keys", "root");
        }
        catch (Exception ex)
        {
            AddAggregateException(ex);
        }
    }

    private bool ValidateRedisKey(CheckKey redisKey)
    {
        try
        {
            ValidateBase(redisKey, $"key ({redisKey.Key})");
            Validate(redisKey, $"key ({redisKey.Key})");
            ValidateKey(redisKey);
            redisKey.SetSize();
            ValidateGreaterThen(redisKey.MemoryUsageNumber, 0, "max memory usage", "keys");
            ValidateGreaterThen(redisKey.Length, 0, "max length", "keys");
            ValidateNoArguments(redisKey);
        }
        catch (Exception ex)
        {
            AddAggregateException(ex);
            return false;
        }

        return true;
    }
}