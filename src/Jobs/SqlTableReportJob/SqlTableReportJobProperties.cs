using CommonJob;
using YamlDotNet.Serialization;

namespace Planar;

public class SqlTableReportJobProperties : IJobProperties, IPathJobProperties, IJobPropertiesWithFiles
{
    [YamlMember(Alias = "path", Order = 0)]
    public string Path { get; set; } = string.Empty;

    [YamlMember(Alias = "connection name", Order = 1)]
    public string? ConnectionName { get; set; }

    [YamlMember(Alias = "filename", Order = 2)]
    public string Filename { get; set; } = null!;

    [YamlMember(Alias = "group", Order = 3)]
    public string Group { get; set; } = null!;

    [YamlMember(Alias = "timeout", Order = 4)]
    public TimeSpan? Timeout { get; set; }

    [YamlMember(Alias = "title", Order = 5)]
    public string? Title { get; set; }

    [YamlIgnore]
    public string FullFilename { get; set; } = null!;

    [YamlIgnore]
    public string Script { get; set; } = string.Empty;

    [YamlIgnore]
    internal string? ConnectionString { get; set; }

    public IEnumerable<string> Files =>
    [
        string.IsNullOrWhiteSpace(Path) ? Filename : System.IO.Path.Combine(Path, Filename)
    ];

    public void SetGlobalConfigPlaceholder(Dictionary<string, string?> parameters)
    {
        // No global config placeholder to set for SqlTableReportJobProperties //
    }
}