namespace RedisCheck;

internal class RedisKeyResult
{
    public long? MemoryUsage { get; set; }
    public long? Length { get; set; }
    public bool? Exists { get; set; }
}