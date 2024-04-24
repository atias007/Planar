using Common;
using Microsoft.Extensions.Configuration;

namespace SqlQueryCheck;

internal class CheckQuery(IConfigurationSection section) : BaseDefault(section), INamedCheckElement
{
    public string Name { get; private set; } = section.GetValue<string>("name") ?? string.Empty;

    public string ConnectionStringName { get; private set; } = section.GetValue<string>("connection string name") ?? string.Empty;

    public string Query { get; private set; } = section.GetValue<string>("query") ?? string.Empty;

    public string? Message { get; private set; } = section.GetValue<string>("message");

    public TimeSpan? Timeout { get; private set; } = section.GetValue<TimeSpan?>("timeout");

    public TimeSpan? Interval { get; private set; } = section.GetValue<TimeSpan?>("interval");

    public string Key => Name;

    public bool Active { get; private set; } = section.GetValue<bool?>("active") ?? true;

    // =================== //

    public string? ConnectionString { get; set; }
}