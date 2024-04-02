using Planar.Common;

namespace CommonJob;

public sealed class JobMonitorUtil(IMonitorUtil monitorUtil, MonitorDurationCache monitorDurationCache)
{
    public IMonitorUtil MonitorUtil { get; private set; } = monitorUtil;

    public MonitorDurationCache MonitorDurationCache { get; private set; } = monitorDurationCache;
}