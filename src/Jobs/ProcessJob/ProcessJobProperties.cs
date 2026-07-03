using CommonJob;
using YamlDotNet.Serialization;

namespace Planar;

/// <summary>
/// https://learn.microsoft.com/en-us/dotnet/api/system.text.encoding.getencodings?view=net-7.0
/// </summary>
public class ProcessJobProperties : BaseProcessJobProperties
{
    [YamlMember(Alias = "arguments", Order = 10)]
    public string? Arguments { get; set; }

    [YamlMember(Alias = "output encoding", Order = 11)]
    public string? OutputEncoding { get; set; }

    [YamlMember(Alias = "log output", Order = 12)]
    public bool LogOutput { get; set; } = true;

    [YamlMember(Alias = "success exit codes", Order = 13)]
    public IEnumerable<int> SuccessExitCodes { get; set; } = [];

    [YamlMember(Alias = "success output regex", Order = 14)]
    public string? SuccessOutputRegex { get; set; }

    [YamlMember(Alias = "fail exit codes", Order = 15)]
    public IEnumerable<int> FailExitCodes { get; set; } = [];

    [YamlMember(Alias = "fail output regex", Order = 16)]
    public string? FailOutputRegex { get; set; }

    public override void SetGlobalConfigPlaceholder(Dictionary<string, string?> parameters)
    {
        base.SetGlobalConfigPlaceholder(parameters);
        Arguments = GetGlobalConfigPropertyPlaceholder(() => Arguments, parameters) ?? Arguments;
    }
}