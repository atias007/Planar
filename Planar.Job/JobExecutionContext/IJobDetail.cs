using System;
using System.Collections.Generic;

namespace Planar.Job
{
    public interface IJobDetail
    {
        bool ConcurrentExecutionDisallowed { get; }
        string Description { get; }
        bool Durable { get; }
        Dictionary<string, string> JobDataMap { get; }
        IKey Key { get; }
        bool PersistJobDataAfterExecution { get; }
        bool RequestsRecovery { get; }
    }
}