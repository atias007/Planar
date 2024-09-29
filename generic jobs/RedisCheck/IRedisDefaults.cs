namespace RedisCheck;

internal interface IRedisDefaults
{
    int? RetryCount { get; }
    TimeSpan? RetryInterval { get; }
    int? Database { get; }
}