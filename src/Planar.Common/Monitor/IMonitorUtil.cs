using Quartz;
using System;

namespace Planar.Common
{
    public interface IMonitorUtil
    {
        void Scan(MonitorEvents @event, IJobExecutionContext context, Exception? exception = null);
    }
}