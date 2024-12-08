using System;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities;

public class PauseResumeJobRequest : JobOrTriggerKey
{
    [YamlMember(Alias = "timeout")]
    public DateTime? AutoResumeDate { get; set; }
}