using CommonJob;
using System.Data;
using YamlDotNet.Serialization;

namespace Planar;

public class SqlJobProperties : IPathJobProperties, IJobPropertiesWithFiles
{
    [YamlMember(Alias = "path", Order = 0)]
    public string Path { get; set; } = string.Empty;

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

    public IEnumerable<string> Files
    {
        get
        {
            if (Steps == null) { return []; }
            var files = Steps
                .Where(s => !string.IsNullOrWhiteSpace(s.Filename))
                .Select(s => s.Filename ?? string.Empty);

            if (!files.Any()) { return []; }
            var result =
                string.IsNullOrWhiteSpace(Path) ?
                files :
                files.Select(f => System.IO.Path.Combine(Path, f));

            return result;
        }
    }
}