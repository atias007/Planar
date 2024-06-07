using Common;
using Cronos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using RedisCheck;

namespace RedisOperations;

internal class Job : BaseCheckJob
{
    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);
        RedisFactory.Initialize(Configuration);

        var keys = GetKeys(Configuration);
        ValidateRequired(keys, "keys");
        ValidateDuplicateKeys(keys, "keys");

        var tasks = SafeInvokeOperation(keys, InvokeKeyCheckInner);
        await Task.WhenAll(tasks);

        CheckAggragateException();
        HandleCheckExceptions();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterBaseCheck();
    }

    private static IEnumerable<RedisKey> GetKeys(IConfiguration configuration)
    {
        var keys = configuration.GetRequiredSection("keys");
        foreach (var item in keys.GetChildren())
        {
            var key = new RedisKey(item);
            ValidateRedisKey(key);
            yield return key;
        }
    }

    private static void ValidateNoArguments(RedisKey redisKey)
    {
        if (!redisKey.IsValid)
        {
            throw new InvalidDataException($"key '{redisKey.Key}' has no arguments to check");
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
            // TODO: do default command
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

    private static void ValidateRedisKey(RedisKey redisKey)
    {
        ValidateGreaterThenOrEquals(redisKey.Database, 0, "database", "keys");
        ValidateLessThenOrEquals(redisKey.Database, 16, "database", "keys");
        ValidateRequired(redisKey.Key, "key", "keys");
        ValidateMaxLength(redisKey.Key, 1024, "key", "keys");
        ValidateMaxLength(redisKey.ExpireCron, 100, "expire cron", "keys");
        ValidateMaxLength(redisKey.DefaultCommand, 1000, "default command", "keys");
        ValidateCron(redisKey);
        ValidateNoArguments(redisKey);
    }

    private static void ValidateCron(RedisKey redisKey)
    {
        if (redisKey.ExpireCron == null) { return; }

        if (!CronExpression.TryParse(redisKey.ExpireCron, out var cron))
        {
            throw new InvalidDataException($"'expire cron' field on 'keys' section is not valid cron expression");
        }

        redisKey.CronExpression = cron;
        var next = cron.GetNextOccurrence(DateTime.Now) ??
            throw new InvalidDataException($"'expire cron' field on 'keys' has no future date");

        redisKey.NextExpireCronDate = next;
    }
}