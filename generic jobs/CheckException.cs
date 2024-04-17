namespace Common;

public sealed class CheckException : Exception
{
    public CheckException(string message, string? key) : base(message)
    {
        Key = key;
    }

    public CheckException(string message, Exception? innerException, string? key) : base(message, innerException)
    {
        Key = key;
    }

    public string? Key { get; }
}