using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Polly;
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
        RedisFactory.Initialize(Configuration);

        var tasks = new List<Task>();
        var defaults = GetDefaults(Configuration);
        var keys = GetKeys(Configuration);
        ValidateKeys(keys);
        CheckAggragateException();

        foreach (var f in keys)
        {
            FillDefaults(f, defaults);
            if (!ValidateRedisKey(f)) { continue; }
            var task = Task.Run(() => SafeInvokeKeyCheck(f));
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        CheckAggragateException();
        HandleCheckExceptions();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.AddSingleton<RedisFailCounter>();
        services.AddSingleton<CheckSpanTracker>();
    }

    private static void FillDefaults(CheckKey redisKey, Defaults defaults)
    {
        // Fill Defaults
        redisKey.Key ??= string.Empty;
        redisKey.Key = redisKey.Key.Trim();
        redisKey.Database ??= defaults.Database;
        FillBase(redisKey, defaults);
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
        var empty = Defaults.Empty;
        var defaults = configuration.GetSection("defaults");
        if (defaults == null)
        {
            Logger.LogWarning("no defaults section found on settings file. set job factory defaults");
            return empty;
        }

        var result = new Defaults
        {
            RetryCount = defaults.GetValue<int?>("retry count") ?? empty.RetryCount,
            RetryInterval = defaults.GetValue<TimeSpan?>("retry interval") ?? empty.RetryInterval,
            MaximumFailsInRow = defaults.GetValue<int?>("maximum fails in row") ?? empty.MaximumFailsInRow,
            Database = defaults.GetValue<int?>("database") ?? empty.Database
        };

        ValidateBase(result, "defaults");
        Validate(result, "defaults");

        return result;
    }

    private async Task InvokeFolderInner(CheckKey key)
    {
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
            throw new CheckException($"key '{key.Key}' length is greater then {key.Length:N0}", key.Key);
        }

        if (key.MemoryUsageNumber > 0 && size > key.MemoryUsageNumber)
        {
            throw new CheckException($"key '{key.Key}' size is greater then {key.MemoryUsage:N0}", key.Key);
        }

        Logger.LogInformation("redis check success for key '{Key}'", key.Key);
    }

    private void SafeHandleException(CheckKey redisKey, Exception ex, RedisFailCounter counter)
    {
        try
        {
            var exception = ex is CheckException ? null : ex;

            if (exception == null)
            {
                Logger.LogError("redis check fail for key '{Key}'. reason: {Message}",
                  redisKey.Key, ex.Message);
            }
            else
            {
                Logger.LogError(exception, "redis check fail for key '{Key}'. reason: {Message}",
                    redisKey.Key, ex.Message);
            }

            var value = counter.IncrementFailCount(redisKey);

            if (value > redisKey.MaximumFailsInRow)
            {
                Logger.LogWarning("redis check fail but maximum fails in row reached for key '{Key}'. reason: {Message}",
                    redisKey.Key, ex.Message);
            }
            else
            {
                var hcException = new CheckException(
                    $"redis check fail for key '{redisKey.Key}'. reason: {ex.Message}",
                    redisKey.Key);

                AddCheckException(hcException);
            }
        }
        catch (Exception innerEx)
        {
            AddAggregateException(innerEx);
        }
    }

    private async Task SafeInvokeKeyCheck(CheckKey key)
    {
        var counter = ServiceProvider.GetRequiredService<RedisFailCounter>();

        try
        {
            if (key.RetryCount == 0)
            {
                await InvokeFolderInner(key);
                return;
            }

            await Policy.Handle<Exception>()
                    .WaitAndRetryAsync(
                        retryCount: key.RetryCount.GetValueOrDefault(),
                        sleepDurationProvider: _ => key.RetryInterval.GetValueOrDefault(),
                         onRetry: (ex, _) =>
                         {
                             var exception = ex is CheckException ? null : ex;
                             Logger.LogWarning(exception, "retry for redis key '{Key}'. Reason: {Message}",
                                                                     key.Key, ex.Message);
                         })
                    .ExecuteAsync(async () =>
                    {
                        await InvokeFolderInner(key);
                    });

            counter.ResetFailCount(key);
        }
        catch (Exception ex)
        {
            SafeHandleException(key, ex, counter);
        }
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