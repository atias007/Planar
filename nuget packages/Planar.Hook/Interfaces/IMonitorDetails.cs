using System;
using System.Collections.Generic;

namespace Planar.Hook
{
    public interface IMonitorDetails : IMonitor
    {
#if NETSTANDARD2_0

        string Author { get; }
        string Calendar { get; }
        string JobDescription { get; }
        IReadOnlyDictionary<string, string> MergedJobDataMap { get; }
        string TriggerGroup { get; }
        string TriggerName { get; }
#else

        string? Author { get; }
        string? Calendar { get; }
        string? JobDescription { get; }
        IReadOnlyDictionary<string, string?> MergedJobDataMap { get; }
        string? TriggerGroup { get; }
        string? TriggerName { get; }
#endif

        bool Durable { get; }
        string FireInstanceId { get; }
        DateTime FireTime { get; }
        string JobGroup { get; }
        string JobId { get; }
        string JobName { get; }
        TimeSpan JobRunTime { get; }
        bool Recovering { get; }
        string TriggerId { get; }
    }
}