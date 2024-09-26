namespace Common;

public interface ICheckElement
{
    string Key { get; }
    bool Active { get; }
    TimeSpan? AllowedFailSpan { get; }
}