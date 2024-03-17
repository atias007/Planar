using Quartz;
using System;
using System.Threading;

namespace Planar.Common;

public interface IMonitorUtil
{
    void Scan(MonitorEvents @event, IJobExecutionContext context, Exception? exception = null, CancellationToken cancellationToken = default);
}