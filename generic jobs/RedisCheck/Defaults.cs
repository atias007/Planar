namespace RedisStreamCheck;

internal class Defaults : IRedisKey
{
    public int? RetryCount { get; set; } = 1;
    public TimeSpan? RetryInterval { get; set; } = TimeSpan.FromSeconds(10);
    public int? MaximumFailsInRow { get; set; } = 5;
    public int? Database { get; set; } = 0;

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}