namespace RedisStreamCheck;

public sealed class RedisCheckException(string message, string? key) : Exception(message)
{
    public string? Key { get; } = key;
}