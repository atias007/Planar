using Common;
using Microsoft.Extensions.Configuration;

namespace InfluxDBCheck;

internal class InfluxQuery(IConfigurationSection section, Defaults defaults) : BaseDefault(section, defaults), INamedCheckElement, IVetoEntity
{
    public string Key => Name;

    public string Name { get; } = section.GetValue<string>("name") ?? string.Empty;
    public string Query { get; } = section.GetValue<string>("query") ?? string.Empty;
    public string RecordsCondition { get; } = section.GetValue<string>("records condition") ?? string.Empty;
    public string ValueCondition { get; } = section.GetValue<string>("value condition") ?? string.Empty;
    public string Message { get; } = section.GetValue<string>("message") ?? string.Empty;
    public TimeSpan Timeout { get; } = section.GetValue<TimeSpan?>("timeout") ?? TimeSpan.FromSeconds(30);
    public TimeSpan? Interval { get; } = section.GetValue<TimeSpan?>("interval");
    public bool Active { get; } = section.GetValue<bool?>("active") ?? true;

    //// -------------------------- ////

    public Condition? InternalRecordsCondition { get; set; }
    public Condition? InternalValueCondition { get; set; }

    //// -------------------------- ////
    public bool Veto { get; set; }

    public string? VetoReason { get; set; }
}