using CommonJob;
using YamlDotNet.Serialization;

namespace Planar;

public class SqlTableReportJobProperties : IPathJobProperties
{
    public string Path { get; set; } = null!;

    [YamlMember(Alias = "connection name")]
    public string? ConnectionName { get; set; }

    public string Filename { get; set; } = null!;
    public string Group { get; set; } = null!;
    public TimeSpan? Timeout { get; set; }
    public string? Title { get; set; }

    [YamlIgnore]
    public string FullFilename { get; set; } = null!;

    [YamlIgnore]
    public string Script { get; set; } = string.Empty;

    [YamlIgnore]
    internal string? ConnectionString { get; set; }
}