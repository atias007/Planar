using Quartz;
using System;
using System.Threading.Tasks;

namespace Planar.Common
{
    public interface IMonitorUtil
    {
        Task Scan(MonitorEvents @event, IJobExecutionContext context, Exception? exception = null);
    }
}