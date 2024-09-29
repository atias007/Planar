namespace Common;

public interface IIntervalEntity
{
    string Key { get; }
    TimeSpan? Interval { get; }
}