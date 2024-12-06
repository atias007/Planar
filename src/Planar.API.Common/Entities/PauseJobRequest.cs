using System;

namespace Planar.API.Common.Entities;

public class PauseJobRequest : JobOrTriggerKey
{
    public DateTime? AutoResumeDate { get; set; }
}