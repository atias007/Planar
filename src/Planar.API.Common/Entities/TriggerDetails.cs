using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities;

public class TriggerDetails : PausedTriggerDetails
{
    [YamlMember(Order = 20)]
    public DateTime Start { get; set; }

    [YamlMember(Order = 21)]
    public DateTime? End { get; set; }

    [YamlMember(Order = 22)]
    public string? CalendarName { get; set; }

    [YamlMember(Order = 23)]
    public TimeSpan? Timeout { get; set; }

    [YamlMember(Order = 24)]
    public TimeSpan? RetrySpan { get; set; }

    [YamlMember(Order = 25)]
    public int? MaxRetries { get; set; }

    [YamlMember(Order = 26)]
    public string? MisfireBehaviour { get; set; }

    [YamlMember(Order = 27)]
    public int Priority { get; set; }

    [YamlMember(Order = 28)]
    public DateTime? NextFireTime { get; set; }

    [YamlMember(Order = 29)]
    public DateTime? PreviousFireTime { get; set; }

    [YamlMember(Order = 30)]
    public bool MayFireAgain { get; set; }

    [YamlMember(Order = 31)]
    public DateTime? FinalFire { get; set; }

    [YamlMember(Order = 32)]
    public string? State { get; set; }

    [YamlMember(Order = 33)]
    public bool Active { get; set; }

    [YamlMember(Order = 100)]
    public Dictionary<string, string?> DataMap { get; set; } = [];
}