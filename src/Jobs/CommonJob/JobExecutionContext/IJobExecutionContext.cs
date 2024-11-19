using System;

// *** DO NOT EDIT NAMESPACE IDENTETION ***

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

        IDataMap MergedJobDataMap { get; }

        string Environment { get; }
    }
}