namespace RedisCheck;

internal interface IRedisDefaults
{
    int? RetryCount { get; }
    TimeSpan? RetryInterval { get; }
    int? MaximumFailsInRow { get; }
    int? Database { get; }
}