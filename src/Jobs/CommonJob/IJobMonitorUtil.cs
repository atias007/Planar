using Planar.Common;

namespace CommonJob
{
    public sealed class JobMonitorUtil
    {
        public JobMonitorUtil(IMonitorUtil monitorUtil, MonitorDurationCache monitorDurationCache)
        {
            MonitorUtil = monitorUtil;
            MonitorDurationCache = monitorDurationCache;
        }

        public IMonitorUtil MonitorUtil { get; private set; }

        public MonitorDurationCache MonitorDurationCache { get; private set; }
    }
}