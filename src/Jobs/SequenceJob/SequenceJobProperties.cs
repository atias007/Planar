using YamlDotNet.Serialization;

namespace Planar;

public class SequenceJobProperties
{
    [YamlMember(Alias = "stop running on fail")]
    public bool StopRunningOnFail { get; set; } = true;

    public List<SequenceJobStep> Steps { get; set; } = [];
}

public class SequenceJobStep
{
    public string Key { get; set; } = null!;

    public TimeSpan? Timeout { get; set; }

    public Dictionary<string, string?> Data { get; set; } = [];
}