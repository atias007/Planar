namespace RestHealthCheck;

public sealed class HealthCheckException : Exception
{
    public HealthCheckException(string message) : base(message)
    {
    }

    public HealthCheckException(string message, Exception innerException) : base(message, innerException)
    {
    }
}