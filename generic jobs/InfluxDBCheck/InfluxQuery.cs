using Common;
using Microsoft.Extensions.Configuration;

namespace InfluxDBCheck;

internal class InfluxQuery(IConfigurationSection section) : BaseDefault(section), INamedCheckElement
{
    public string Name { get; set; } = section.GetValue<string>("name") ?? string.Empty;
    public string Key => Name;
    public string Query { get; private set; } = section.GetValue<string>("query") ?? string.Empty;
    public string RecordsCondition { get; private set; } = section.GetValue<string>("records condition") ?? string.Empty;
    public string ValueCondition { get; private set; } = section.GetValue<string>("value condition") ?? string.Empty;
    public string Message { get; private set; } = section.GetValue<string>("message") ?? string.Empty;
    public TimeSpan Timeout { get; private set; } = section.GetValue<TimeSpan?>("timeout") ?? TimeSpan.FromSeconds(30);
    public TimeSpan? Interval { get; private set; } = section.GetValue<TimeSpan?>("interval");
    public bool Active { get; private set; } = section.GetValue<bool?>("active") ?? true;

    // -------------------------- //

    public Condition? InternalRecordsCondition { get; set; }
    public Condition? InternalValueCondition { get; set; }
}