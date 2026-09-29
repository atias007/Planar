using YamlDotNet.Serialization;

namespace Planar;

public enum EffectedRowsSourceMembers
{
    Default,
    Scalar,
    None
}

public sealed class SqlStep
{
    [YamlMember(Alias = "connection name")]
    public string? ConnectionName { get; set; }

    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    [YamlMember(Alias = "query resource")]
    public string? QueryResource { get; set; }

    public TimeSpan? Timeout { get; set; }

    [YamlMember(Alias = "effected rows source")]
    public string? EffectedRowsSource { get; set; } = nameof(EffectedRowsSourceMembers.Default);

    [YamlMember(Alias = "log result set")]
    public bool LogResultSet { get; set; } = false;

    [YamlIgnore]
    public string Script { get; set; } = string.Empty;

    [YamlIgnore]
    internal string? ConnectionString { get; set; }

    public EffectedRowsSourceMembers GetEffectedRowsSource()
    {
        if (EffectedRowsSource == null) { return EffectedRowsSourceMembers.Default; }
        if (!Enum.TryParse<EffectedRowsSourceMembers>(EffectedRowsSource.Trim(), true, out var result))
        {
            return EffectedRowsSourceMembers.Default;
        }
        return result;
    }
}