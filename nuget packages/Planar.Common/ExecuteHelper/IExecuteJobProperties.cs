using System;
using System.Collections.Generic;

namespace Planar.Common
{
    public interface IExecuteJobProperties
    {
        TimeSpan? CancelJobAfter { get; }
        string Environment { get; }
        DateTimeOffset? ExecutionDate { get; }
        Dictionary<string, string?> GlobalSettings { get; }
        Dictionary<string, object?> JobData { get; }
        string JobKeyGroup { get; }
        string JobKeyName { get; }
        Type? JobType { get; }
        bool Recovering { get; }
        int RefireCount { get; }
        Dictionary<string, object?> TriggerData { get; }
        string TriggerKeyGroup { get; }
        string TriggerKeyName { get; }
    }
}