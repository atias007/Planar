using System;

// *** DO NOT EDIT NAMESPACE IDENTETION ***

namespace Planar.Job
{
    public interface ITriggerDetail
    {
#if NETSTANDARD2_0
        string CalendarName { get; }
        string Description { get; }
#else
        string? CalendarName { get; }
        string? Description { get; }
#endif

        DateTimeOffset? EndTime { get; }
        DateTimeOffset? FinalFireTime { get; }
        bool HasMillisecondPrecision { get; }
        IKey Key { get; }
        int Priority { get; }
        DateTimeOffset StartTime { get; }
        IDataMap TriggerDataMap { get; }
        bool HasRetry { get; }
        TimeSpan? RetrySpan { get; }
        bool? IsLastRetry { get; }
        bool IsRetryTrigger { get; }
        int? RetryNumber { get; }
        int? MaxRetries { get; }
        string Id { get; }
        TimeSpan? Timeout { get; }
    }
}