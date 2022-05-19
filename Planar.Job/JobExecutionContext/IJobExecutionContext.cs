using Planar.Job;
using System;
using System.Collections.Generic;

namespace Planar
{
    public interface IJobExecutionContext
    {
        string FireInstanceId { get; }

        DateTimeOffset FireTime { get; }

        DateTimeOffset? NextFireTime { get; }

        DateTimeOffset? ScheduledFireTime { get; }

        DateTimeOffset? PreviousFireTime { get; }

        bool Recovering { get; }

        int RefireCount { get; }

        // Calendar

        IJobDetail JobDetails { get; }

        // TriggerDetails

        Dictionary<string, string> MergedJobDataMap { get; }

        Key RecoveringTriggerKey { get; }

        Dictionary<string, string> JobSettings { get; }
    }
}