using YamlDotNet.Serialization;

namespace Planar;

public class SequenceJobProperties
{
    [YamlMember(Alias = "stop running on fail", Order = 0)]
    public bool StopRunningOnFail { get; set; } = true;

    [YamlMember(Alias = "steps", Order = 1)]
    public List<SequenceJobStep> Steps { get; set; } = [];
}

public class SequenceJobStep
{
    [YamlMember(Alias = "key", Order = 0)]
    public string Key { get; set; } = null!;

    [YamlMember(Alias = "timeout", Order = 1)]
    public TimeSpan? Timeout { get; set; }

    [YamlMember(Alias = "data", Order = 2)]
    public Dictionary<string, string?> Data { get; set; } = [];
}