namespace Common;

public interface ICheckElement
{
    string Key { get; }
    bool Active { get; }
    TimeSpan? Span { get; }
}