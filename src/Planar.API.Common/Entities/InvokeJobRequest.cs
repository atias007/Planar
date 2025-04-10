using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities;

public class InvokeJobRequest : JobOrTriggerKey
{
    [YamlMember(Alias = "now override value")]
    public DateTime? NowOverrideValue { get; set; }

    [YamlMember(Alias = "timeout")]
    public TimeSpan? Timeout { get; set; }

    [YamlMember(Alias = "data")]
    public Dictionary<string, string?>? Data { get; set; }
}