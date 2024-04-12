namespace RedisStreamCheck;

internal interface IRedisKey
{
    int? RetryCount { get; set; }
    int? MaximumFailsInRow { get; set; }
    TimeSpan? RetryInterval { get; set; }
    public int? Database { get; set; }
}