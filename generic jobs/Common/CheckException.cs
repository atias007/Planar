namespace Common;

public sealed class CheckException : Exception
{
#pragma warning disable CA1822 // Mark members as static
    public bool HideStackTraceFromPlanar => true;
#pragma warning restore CA1822 // Mark members as static

    public CheckException(string message) : base(message)
    {
    }

    public CheckException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}