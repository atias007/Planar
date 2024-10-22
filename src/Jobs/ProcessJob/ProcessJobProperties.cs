using CommonJob;
using YamlDotNet.Serialization;

namespace Planar;

/// <summary>
/// https://learn.microsoft.com/en-us/dotnet/api/system.text.encoding.getencodings?view=net-7.0
/// </summary>
public class ProcessJobProperties : BaseProcessJobProperties, IFileJobProperties
{
    public string Path { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string? Arguments { get; set; }

    [YamlMember(Alias = "output encoding")]
    public string? OutputEncoding { get; set; }

    [YamlMember(Alias = "log output")]
    public bool LogOutput { get; set; } = true;

    [YamlMember(Alias = "success exit codes")]
    public IEnumerable<int> SuccessExitCodes { get; set; } = [];

    [YamlMember(Alias = "success output regex")]
    public string? SuccessOutputRegex { get; set; }

    [YamlMember(Alias = "fail exit codes")]
    public IEnumerable<int> FailExitCodes { get; set; } = [];

    [YamlMember(Alias = "fail output regex")]
    public string? FailOutputRegex { get; set; }
}