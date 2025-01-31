using System;
using System.Collections.Generic;

namespace Planar.Common
{
    public interface IExecuteJobProperties
    {
#if NETSTANDARD2_0
        IDictionary<string, string> GlobalSettings { get; }
        IDictionary<string, object> JobData { get; }
        IDictionary<string, object> TriggerData { get; }
#else
        IDictionary<string, string?> GlobalSettings { get; }
        IDictionary<string, object?> JobData { get; }
        IDictionary<string, object?> TriggerData { get; }
#endif

        string Environment { get; }
        DateTimeOffset? ExecutionDate { get; }

        string JobKeyGroup { get; }
        string JobKeyName { get; }
        bool Recovering { get; }
        int RefireCount { get; }
        string TriggerKeyGroup { get; }
        string TriggerKeyName { get; }
        TimeSpan? TriggerTimeout { get; }
    }
}