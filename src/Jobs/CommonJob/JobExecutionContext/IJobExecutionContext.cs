using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;

namespace Planar.Job
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

        IJobDetail JobDetails { get; }

        ITriggerDetail TriggerDetails { get; }

        SortedDictionary<string, string?> MergedJobDataMap { get; }

        string Environment { get; }
    }
}