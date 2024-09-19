namespace Common;

public sealed class CheckException : Exception
{
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable S2325 // Methods and properties that don't access instance data should be static
    public bool HideStackTraceFromPlanar => true;
#pragma warning restore S2325 // Methods and properties that don't access instance data should be static
#pragma warning restore CA1822 // Mark members as static

    public CheckException(string message) : base(message)
    {
    }

    public CheckException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}