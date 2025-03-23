namespace Planar;

public class SequenceJobProperties
{
    public List<SequenceJobStep> Steps { get; set; } = [];
}

public class SequenceJobStep
{
    public string Key { get; set; } = null!;

    public TimeSpan? Timeout { get; set; }

    public Dictionary<string, string?> Data { get; set; } = [];
}