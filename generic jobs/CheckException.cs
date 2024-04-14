namespace Common;

public sealed class CheckException(string message, string? key) : Exception(message)
{
    public string? Key { get; } = key;
}