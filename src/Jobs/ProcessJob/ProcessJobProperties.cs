using CommonJob;
using YamlDotNet.Serialization;

namespace Planar;

/// <summary>
/// https://learn.microsoft.com/en-us/dotnet/api/system.text.encoding.getencodings?view=net-7.0
/// </summary>
public class ProcessJobProperties : BaseProcessJobProperties, IFileJobProperties
{
    [YamlMember(Alias = "path", Order = 0)]
    public string Path { get; set; } = string.Empty;
    [YamlMember(Alias = "filename", Order = 1)]
    public string Filename { get; set; } = string.Empty;
    [YamlMember(Alias = "arguments", Order = 2)]
    public string? Arguments { get; set; }

    [YamlMember(Alias = "output encoding", Order = 3)]
    public string? OutputEncoding { get; set; }

    [YamlMember(Alias = "log output", Order = 4)]
    public bool LogOutput { get; set; } = true;

    [YamlMember(Alias = "success exit codes", Order = 5)]
    public IEnumerable<int> SuccessExitCodes { get; set; } = [];

    [YamlMember(Alias = "success output regex", Order = 6)]
    public string? SuccessOutputRegex { get; set; }

    [YamlMember(Alias = "fail exit codes", Order = 7)]
    public IEnumerable<int> FailExitCodes { get; set; } = [];

    [YamlMember(Alias = "fail output regex", Order = 8)]
    public string? FailOutputRegex { get; set; }

    public IEnumerable<string> Files =>
    [
        string.IsNullOrWhiteSpace(Path) ? Filename : System.IO.Path.Combine(Path, Filename)
    ];
}