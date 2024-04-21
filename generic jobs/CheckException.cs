namespace Common;

public sealed class CheckException : Exception
{
    public CheckException(string message) : base(message)
    {
    }

    public CheckException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}