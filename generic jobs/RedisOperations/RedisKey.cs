using Common;
using Cronos;
using Microsoft.Extensions.Configuration;
using RedisCheck;

namespace RedisOperations;

internal class RedisKey(IConfigurationSection section) : ICheckElement, IRedisKey
{
    public string Key { get; set; } = section.GetValue<string>("key") ?? string.Empty;
    public string? ExpireCron { get; set; } = section.GetValue<string>("expire cron");
    public string? DefaultCommand { get; set; } = section.GetValue<string>("default command");
    public int? Database { get; set; } = section.GetValue<int>("database");
    public bool Active { get; private set; } = section.GetValue<bool?>("active") ?? true;
    public bool IsValid => !string.IsNullOrWhiteSpace(ExpireCron) || !string.IsNullOrWhiteSpace(DefaultCommand);
    public TimeSpan? Span => null;

    public CronExpression? CronExpression { get; set; }
    public DateTime? NextExpireCronDate { get; set; }
}