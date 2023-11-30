using System;
using System.Collections.Generic;

namespace Planar.Common
{
    public interface IExecuteJobProperties
    {
        string Environment { get; }
        DateTimeOffset? ExecutionDate { get; }
        IDictionary<string, string?> GlobalSettings { get; }
        IDictionary<string, object?> JobData { get; }
        string JobKeyGroup { get; }
        string JobKeyName { get; }
        bool Recovering { get; }
        int RefireCount { get; }
        IDictionary<string, object?> TriggerData { get; }
        string TriggerKeyGroup { get; }
        string TriggerKeyName { get; }
    }
}