using CommonJob;
using System.Data;
using YamlDotNet.Serialization;

namespace Planar;

public class SqlJobProperties : IJobProperties, IResourceJobProperties
{
    [YamlMember(Alias = "default connection name", Order = 1)]
    public string? DefaultConnectionName { get; set; }

    [YamlMember(Alias = "transaction", Order = 2)]
    public bool Transaction { get; set; }

    [YamlMember(Alias = "transaction isolation level", Order = 3)]
    public IsolationLevel? TransactionIsolationLevel { get; set; }

    [YamlMember(Alias = "continue on error", Order = 4)]
    public bool ContinueOnError { get; set; }

    [YamlMember(Alias = "steps", Order = 5)]
    public List<SqlStep>? Steps { get; set; } = [];

    [YamlIgnore]
    internal string? DefaultConnectionString { get; set; }

    public IEnumerable<string> ResourceNames
    {
        get
        {
            if (Steps == null) { return []; }
            var files = Steps
                .Where(s => !string.IsNullOrWhiteSpace(s.QueryResource))
                .Select(s => s.QueryResource ?? string.Empty);

            return files;
        }
    }

    public void FillGlobalConfigPlaceholder(Dictionary<string, string?> parameters)
    {
        // No global config placeholder to set for SqlJobProperties //
    }
}