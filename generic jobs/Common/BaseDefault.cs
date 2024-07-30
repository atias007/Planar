using Microsoft.Extensions.Configuration;

namespace Common;

public abstract class BaseDefault
{
    protected BaseDefault()
    {
    }

    protected BaseDefault(IConfigurationSection section, BaseDefault baseDefault)
    {
        RetryCount = section.GetValue<int?>("retry count") ?? baseDefault.RetryCount;
        RetryInterval = section.GetValue<TimeSpan?>("retry interval") ?? baseDefault.RetryInterval;
        MaximumFailsInRow = section.GetValue<int?>("maximum fails in row") ?? baseDefault.MaximumFailsInRow;
        Span = section.GetValue<TimeSpan?>("span") ?? baseDefault.Span;
    }

    public int? RetryCount { get; set; }
    public TimeSpan? RetryInterval { get; set; }
    public int? MaximumFailsInRow { get; set; }
    public TimeSpan? Span { get; set; }
}