using System;

namespace Planar.Job
{
    public interface ITriggerDetail
    {
        string? CalendarName { get; }
        string? Description { get; }
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
    }
}