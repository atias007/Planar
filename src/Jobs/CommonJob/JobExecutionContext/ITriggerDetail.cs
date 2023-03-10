using System;
using System.Collections.Generic;

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
        SortedDictionary<string, string?> TriggerDataMap { get; }
        bool HasRetry { get; }
        bool? IsLastRetry { get; }
        bool IsRetryTrigger { get; }
    }
}