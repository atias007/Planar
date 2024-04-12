using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Polly;
using RedisStream;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Net;

namespace RedisStreamCheck;

internal class Job : BaseJob
{
    private readonly ConcurrentQueue<RedisCheckException> _exceptions = new();

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        RedisFactory.Initialize(Configuration);

        var tasks = new List<Task>();
        var defaults = GetDefaults(Configuration);
        var keys = GetKeys(Configuration);
        await ValidateKeys(keys);
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
        CheckFolderCheckExceptions();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.AddSingleton<RedisFailCounter>();
    }

    private static void FillDefaults(RedisKeyCheck redisKey, Defaults defaults)
    {
        // Fill Defaults
        redisKey.Key ??= string.Empty;
        redisKey.Key = redisKey.Key.Trim();
        redisKey.RetryCount ??= defaults.RetryCount;
        redisKey.Database ??= defaults.Database;
        redisKey.MaximumFailsInRow ??= defaults.MaximumFailsInRow;
        redisKey.RetryInterval ??= defaults.RetryInterval;
    }

    private static IEnumerable<RedisKeyCheck> GetKeys(IConfiguration configuration)
    {
        var keys = configuration.GetRequiredSection("keys");
        foreach (var item in keys.GetChildren())
        {
            var folder = new RedisKeyCheck(item);
            yield return folder;
        }
    }

    private static void Validate(IRedisKey redisKey, string section)
    {
        if ((redisKey.RetryInterval?.TotalSeconds ?? 0) < 1)
        {
            throw new InvalidDataException($"'retry interval' on {section} section is null or less then 1 second");
        }

        if ((redisKey.RetryInterval?.TotalMinutes ?? 0) > 1)
        {
            throw new InvalidDataException($"'retry interval' on {section} section is greater then 1 minutes");
        }

        if (redisKey.RetryCount < 0)
        {
            throw new InvalidDataException($"'retry count' on {section} section is null or less then 0");
        }

        if (redisKey.RetryCount > 10)
        {
            throw new InvalidDataException($"'retry count' on {section} section is greater then 0");
        }

        if (redisKey.MaximumFailsInRow < 1)
        {
            throw new InvalidDataException($"'maximum fails in row' on {section} section must be greater then 0");
        }

        if (redisKey.MaximumFailsInRow > 1000)
        {
            throw new InvalidDataException($"'maximum fails in row' on {section} section must be less then 1000");
        }

        if (redisKey.Database < 0)
        {
            throw new InvalidDataException($"'database' on {section} section must be greater then 0");
        }

        if (redisKey.Database > 16)
        {
            throw new InvalidDataException($"'database' on {section} section must be less then 16");
        }
    }

    private static void ValidateKey(RedisKeyCheck redisKey)
    {
        if (string.IsNullOrWhiteSpace(redisKey.Key))
        {
            throw new InvalidDataException("key on keys section is null or empty");
        }

        if (redisKey.Key?.Length > 1024)
        {
            throw new InvalidDataException($"'key' length ({redisKey.Key[0..20]}...) must be less then 1024");
        }
    }

    private static void ValidateLength(RedisKeyCheck redisKey)
    {
        if (redisKey.MaxLength <= 0)
        {
            throw new InvalidDataException($"'max length' on key '{redisKey.Key}' must be greater then 0");
        }

        if (redisKey.MinLength <= 0)
        {
            throw new InvalidDataException($"'min length' on key '{redisKey.Key}' must be greater then 0");
        }

        if (redisKey.MaxLength <= redisKey.MinLength)
        {
            throw new InvalidDataException($"'max length' on key '{redisKey.Key}' must be greater then 'min length'");
        }
    }

    private static void ValidateMemoryUsage(RedisKeyCheck redisKey)
    {
        redisKey.SetSize();
        if (redisKey.MaxMemoryUsageNumber <= 0)
        {
            throw new InvalidDataException($"'max memory usage' on key '{redisKey.Key}' must be greater then 0");
        }

        if (redisKey.MinMemoryUsageNumber <= 0)
        {
            throw new InvalidDataException($"'max memory usage' on key '{redisKey.Key}' must be greater then 0");
        }

        if (redisKey.MaxMemoryUsageNumber <= redisKey.MinMemoryUsageNumber)
        {
            throw new InvalidDataException($"'max memory usage' on key '{redisKey.Key}' must be greater then 'min memory usage'");
        }
    }

    private static void ValidateNoArguments(RedisKeyCheck redisKey)
    {
        if (!redisKey.IsValid)
        {
            throw new InvalidDataException($"key '{redisKey.Key}' has no arguments to check");
        }
    }

    private void CheckFolderCheckExceptions()
    {
        if (!_exceptions.IsEmpty)
        {
            var message = $"redis check failed for keys: {string.Join(", ", _exceptions.Select(x => x.Key).Distinct())}";
            throw new AggregateException(message, _exceptions);
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

        Validate(result, "defaults");

        return result;
    }

    private async Task InvokeFolderInner(RedisKeyCheck key)
    {
        await RedisFactory.Exists(key);

        long length = 0;
        long size = 0;
        if (key.MaxLength > 0 || key.MinLength > 0)
        {
            length = await RedisFactory.GetLength(key);
            Logger.LogInformation("key '{Key}' length is {Length:N0}", key.Key, length);
        }

        if (key.MaxMemoryUsageNumber > 0 || key.MinMemoryUsageNumber > 0)
        {
            size = await RedisFactory.GetMemoryUsage(key);
            Logger.LogInformation("key '{Key}' size is {Size:N0} byte(s)", key.Key, size);
        }

        if (key.MaxLength > 0 && length > key.MaxLength)
        {
            throw new RedisCheckException($"key '{key.Key}' length is greater then {key.MaxLength:N0}", key.Key);
        }

        if (key.MinLength > 0 && length < key.MinLength)
        {
            throw new RedisCheckException($"key '{key.Key}' length is less then {key.MinLength:N0}", key.Key);
        }

        if (key.MaxMemoryUsageNumber > 0 && size > key.MaxMemoryUsageNumber)
        {
            throw new RedisCheckException($"key '{key.Key}' size is greater then {key.MaxMemoryUsage:N0}", key.Key);
        }

        if (key.MinMemoryUsageNumber > 0 && size < key.MinMemoryUsageNumber)
        {
            throw new RedisCheckException($"key '{key.Key}' size is less then {key.MinMemoryUsage:N0}", key.Key);
        }

        Logger.LogInformation("redis check success for key '{Key}'", key.Key);
    }

    private void SafeHandleException(RedisKeyCheck redisKey, Exception ex, RedisFailCounter counter)
    {
        try
        {
            var exception = ex is RedisCheckException ? null : ex;

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
                var hcException = new RedisCheckException(
                    $"redis check fail for key '{redisKey.Key}'. reason: {ex.Message}",
                    redisKey.Key);

                _exceptions.Enqueue(hcException);
            }
        }
        catch (Exception innerEx)
        {
            AddAggregateException(innerEx);
        }
    }

    private async Task SafeInvokeKeyCheck(RedisKeyCheck key)
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
                             var exception = ex is RedisCheckException ? null : ex;
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

    private async Task ValidateKeys(IEnumerable<RedisKeyCheck> keys)
    {
        try
        {
            if (keys == null || !keys.Any())
            {
                throw new InvalidDataException("keys section is null or empty");
            }

            var duplicates1 = keys.GroupBy(x => x.Key).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
            if (duplicates1.Count != 0)
            {
                throw new InvalidDataException($"duplicated keys found: {string.Join(", ", duplicates1)}");
            }

            await RedisFactory.Ping();
        }
        catch (Exception ex)
        {
            AddAggregateException(ex);
        }
    }

    private bool ValidateRedisKey(RedisKeyCheck redisKey)
    {
        try
        {
            Validate(redisKey, $"key ({redisKey.Key})");
            ValidateKey(redisKey);
            ValidateMemoryUsage(redisKey);
            ValidateLength(redisKey);
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