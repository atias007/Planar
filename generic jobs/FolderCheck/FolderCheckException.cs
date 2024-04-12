namespace FolderCheck;

public sealed class FolderCheckException(string message, string? name) : Exception(message)
{
    public string? Name { get; } = name;
}