using Microsoft.Extensions.Configuration;

namespace Common;

public abstract class BaseDefault
{
    protected BaseDefault()
    {
    }

    protected BaseDefault(BaseDefault baseDefault)
    {
        RetryCount = baseDefault.RetryCount;
        RetryInterval = baseDefault.RetryInterval;
        Span = baseDefault.Span;
    }

    protected BaseDefault(IConfigurationSection section, BaseDefault baseDefault)
    {
        RetryCount = section.GetValue<int?>("retry count") ?? baseDefault.RetryCount;
        RetryInterval = section.GetValue<TimeSpan?>("retry interval") ?? baseDefault.RetryInterval;
        Span = section.GetValue<TimeSpan?>("allowed fail span") ?? baseDefault.Span;
    }

    public int? RetryCount { get; set; }
    public TimeSpan? RetryInterval { get; set; }
    public TimeSpan? Span { get; set; }
}