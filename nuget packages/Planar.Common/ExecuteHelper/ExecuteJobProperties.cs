using System;
using System.Collections.Generic;

namespace Planar.Common
{
    internal class ExecuteJobProperties : IExecuteJobProperties
    {
        private const string DefaultEnvironment = "Unknown";
        private const string DefaultGroup = "Default";
        private const string DefaultJobName = "TestJob";
        private const string DefaultTriggerName = "TestTrigger";

#if NETSTANDARD2_0
        public Type JobType { get; set; }
#else
        public Type? JobType { get; set; }
#endif

        public DateTimeOffset? ExecutionDate { get; set; }

        public bool Recovering { get; set; }

        public int RefireCount { get; set; }

        public string Environment { get; set; } = DefaultEnvironment;

        public string JobKeyName { get; set; } = DefaultJobName;
        public string JobKeyGroup { get; set; } = DefaultGroup;
        public string TriggerKeyName { get; set; } = DefaultTriggerName;
        public string TriggerKeyGroup { get; set; } = DefaultGroup;
        public TimeSpan? TriggerTimeout { get; set; }

#if NETSTANDARD2_0
        public IDictionary<string, object> TriggerData { get; set; } = new Dictionary<string, object>();
        public IDictionary<string, object> JobData { get; set; } = new Dictionary<string, object>();
        public IDictionary<string, string> GlobalSettings { get; set; } = new Dictionary<string, string>();
#else
        public IDictionary<string, object?> TriggerData { get; set; } = new Dictionary<string, object?>();
        public IDictionary<string, object?> JobData { get; set; } = new Dictionary<string, object?>();
        public IDictionary<string, string?> GlobalSettings { get; set; } = new Dictionary<string, string?>();
#endif
    }
}