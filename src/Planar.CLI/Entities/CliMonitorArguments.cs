using YamlDotNet.Serialization;

namespace Planar.CLI.Entities;

public class CliMonitorArguments
{
    public CliMonitorArguments()
    {
    }

    public CliMonitorArguments(int? x, int? y)
    {
        X = x;
        Y = y;
    }

    [YamlMember(Alias = "x")]
    public int? X { get; set; }
    [YamlMember(Alias = "y")]
    public int? Y { get; set; }
}
