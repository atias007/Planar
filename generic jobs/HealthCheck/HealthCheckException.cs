namespace HealthCheck;

public sealed class HealthCheckException(string message, string? name) : Exception(message)
{
    public string? Name { get; } = name;
}