using System;
using System.Collections.Generic;

namespace Planar.Monitor.Hook
{
    public interface IMonitorDetails : IMonitor
    {
        string? Author { get; }
        string? Calendar { get; }
        bool Durable { get; }
        string FireInstanceId { get; }
        DateTime FireTime { get; }
        string? JobDescription { get; }
        string JobGroup { get; }
        string JobId { get; }
        string JobName { get; }
        TimeSpan JobRunTime { get; }
        Dictionary<string, string> MergedJobDataMap { get; }
        bool Recovering { get; }
        string? TriggerDescription { get; }
        string? TriggerGroup { get; }
        string TriggerId { get; }
        string? TriggerName { get; }
    }
}