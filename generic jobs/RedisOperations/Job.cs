using Common;
using Cronos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.CLI.General;
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
        var done = false;
        if (!key.Active)
        {
            Logger.LogInformation("skipping inactive key '{Key}'", key.Key);
            return;
        }

        var exists = await RedisFactory.Exists(key);

        if (!exists && !string.IsNullOrWhiteSpace(key.DefaultCommand))
        {
            var commands = CommandSplitter.SplitCommandLine(key.DefaultCommand).ToList();
            if (commands.Count > 0)
            {
                var result =
                    commands.Count == 1 ?
                    await RedisFactory.Invoke(key, commands[0]) :
                    await RedisFactory.Invoke(key, commands[0], commands[1..]);

                done = true;
                Logger.LogInformation("execute default command '{Command}' for key '{Key}'. result: {Result}", key.DefaultCommand, key.Key, result);
                exists = await RedisFactory.Exists(key);
            }
        }

        if (!exists)
        {
            if (key.Mandatory)
            {
                throw new CheckException($"key '{key.Key}' is mandatory and it is not exists");
            }

            if (!done) { Logger.LogInformation("no action for key '{Key}'", key.Key); }
            return;
        }

        if (key.CronExpression != null && key.NextExpireCronDate != null)
        {
            var setexpire = await RedisFactory.SetExpire(key, key.NextExpireCronDate.Value.ToUniversalTime());
            if (setexpire)
            {
                done = true;
                Logger.LogInformation("set expire date {Date} for key '{Key}'", key.NextExpireCronDate, key.Key);
            }
        }

        if (!done) { Logger.LogInformation("no action for key '{Key}'", key.Key); }
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

        if (!CronExpression.TryParse(redisKey.ExpireCron, CronFormat.IncludeSeconds, out var cron))
        {
            throw new InvalidDataException($"'expire cron' field on 'keys' section with value '{redisKey.ExpireCron}' is not valid cron expression");
        }

        redisKey.CronExpression = cron;

        var next = cron.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Local) ??
            throw new InvalidDataException($"'expire cron' field on 'keys' has no future date");

        redisKey.NextExpireCronDate = next.Date;
    }
}