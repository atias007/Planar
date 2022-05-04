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

        // JobDetails

        // TriggerDetails

        // MergedJobDataMap

        // RecoveringTriggerKey

        Dictionary<string, string> JobSettings { get; }
    }
}