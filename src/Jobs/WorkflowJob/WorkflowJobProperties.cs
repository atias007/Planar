using CommonJob;
using YamlDotNet.Serialization;

namespace Planar;

public class WorkflowJobProperties
{
    public List<WorkflowJobStep> Steps { get; set; } = [];
}

public class WorkflowJobStep
{
    public string Key { get; set; } = null!;

    [YamlMember(Alias = "depends on key")]
    public string? DependsOnKey { get; set; }

    [YamlMember(Alias = "depends on event")]
    public WorkflowJobStepEvent? DependsOnEvent { get; set; }

    public TimeSpan? Timeout { get; set; }

    public Dictionary<string, string?> Data { get; set; } = [];
}