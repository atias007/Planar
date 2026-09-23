using Planar.API.Common.Entities;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace Planar.Service.Model;

internal class JobDataRequest : IApplyRequest
{
    [YamlMember(Alias = "source")]
    public string Source { get; set; } = string.Empty;

    [YamlMember(Alias = "kind")]
    public string Kind { get; set; } = string.Empty;

    [YamlMember(Alias = "version")]
    public string Version { get; set; } = string.Empty;

    [YamlMember(Alias = "job name")]
    public string JobName { get; set; } = string.Empty;

    [YamlMember(Alias = "job group")]
    public string JobGroup { get; set; } = string.Empty;

    [YamlMember(Alias = "job data")]
    public Dictionary<string, string?> JobData { get; set; } = [];

    [YamlMember(Alias = "triggers data")]
    public List<TriggerData> TriggersData { get; set; } = [];

    [YamlIgnore]
    internal IJobDetail JobDetail { get; set; } = null!;

    [YamlIgnore]
    internal IReadOnlyCollection<ITrigger> Triggers { get; set; } = null!;

    public ITrigger? GetTrigger(string name)
    {
        return Triggers.FirstOrDefault(tr => string.Equals(tr.Key.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}

internal class TriggerData
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;

    [YamlMember(Alias = "data")]
    public Dictionary<string, string?> Data { get; set; } = [];
}