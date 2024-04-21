using Microsoft.Extensions.Configuration;

namespace Common;

public abstract class BaseDefault
{
    protected BaseDefault()
    {
    }

    protected BaseDefault(IConfigurationSection section)
    {
        RetryCount = section.GetValue<int?>("retry count");
        RetryInterval = section.GetValue<TimeSpan?>("retry interval");
        MaximumFailsInRow = section.GetValue<int?>("maximum fails in row");
        Span = section.GetValue<TimeSpan?>("span");
    }

    public int? RetryCount { get; set; }
    public TimeSpan? RetryInterval { get; set; }
    public int? MaximumFailsInRow { get; set; }
    public TimeSpan? Span { get; set; }
}