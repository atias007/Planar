namespace Common;

public abstract class BaseDefault
{
    public int? RetryCount { get; set; }
    public TimeSpan? RetryInterval { get; set; }
    public int? MaximumFailsInRow { get; set; }
}