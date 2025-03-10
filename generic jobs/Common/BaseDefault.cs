using Microsoft.Extensions.Configuration;

namespace Common;

public abstract class BaseDefault : BaseActiveElement
{
    protected BaseDefault()
    {
    }

    protected BaseDefault(BaseDefault baseDefault) : base(baseDefault)
    {
        RetryCount = baseDefault.RetryCount;
        RetryInterval = baseDefault.RetryInterval;
        AllowedFailSpan = baseDefault.AllowedFailSpan;
        BindToTriggers = baseDefault.BindToTriggers;
    }

    protected BaseDefault(IConfigurationSection section, BaseDefault baseDefault) : base(section)
    {
        RetryCount = section.GetValue<int?>("retry count") ?? baseDefault.RetryCount;
        RetryInterval = section.GetValue<TimeSpan?>("retry interval") ?? baseDefault.RetryInterval;
        AllowedFailSpan = section.GetValue<TimeSpan?>("allowed fail span") ?? baseDefault.AllowedFailSpan;
        BindToTriggers = section.GetSection("bind to triggers").Get<string[]?>();
    }

    public int? RetryCount { get; set; }
    public TimeSpan? RetryInterval { get; set; }
    public TimeSpan? AllowedFailSpan { get; set; }
    public IEnumerable<string>? BindToTriggers { get; set; }

    //// --------------------------------------------------------------- ////

    public bool Veto { get; set; }
    public string? VetoReason { get; set; }
    public string? ResultMessage { get; set; }
    public virtual CheckStatus RunStatus { get; set; }
}