using Common;
using Microsoft.Extensions.Configuration;

namespace SqlTableRetention;

internal class Table(IConfigurationSection section) : INamedCheckElement
{
    public string Name { get; private set; } = section.GetValue<string>("name") ?? string.Empty;

    public string ConnectionStringName { get; private set; } = section.GetValue<string>("connection string name") ?? string.Empty;

    public string Schema { get; private set; } = section.GetValue<string>("schema") ?? string.Empty;

    public string TableName { get; private set; } = section.GetValue<string>("table name") ?? string.Empty;

    public string? Condition { get; private set; } = section.GetValue<string>("condition") ?? string.Empty;

    public TimeSpan Timeout { get; private set; } = section.GetValue<TimeSpan?>("timeout") ?? TimeSpan.FromMinutes(10);

    public int BatchSize { get; set; } = section.GetValue<int?>("batch size") ?? 10_000;

    public bool Active { get; private set; } = section.GetValue<bool?>("active") ?? true;

    public string Key => Name;

    public TimeSpan? Span => null;

    // =================== //

    public string? ConnectionString { get; set; }
}