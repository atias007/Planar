using Planar.Common;
using Planar.Service.Model;
using Quartz;
using System;

namespace Planar.Service.Monitor;

public enum MonitorScanType
{
    ScanJob,
    ScanSystem,
    ExecuteJob,
    ExecuteSystem
}

public class MonitorScanMessage
{
    public MonitorScanMessage(MonitorEvents @event, IJobExecutionContext context, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(context);
        Event = @event;
        JobExecutionContext = context;
        Exception = exception;
        Type = MonitorScanType.ScanJob;
    }

    public MonitorScanMessage(MonitorEvents @event, MonitorSystemInfo info, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(info);
        Event = @event;
        MonitorSystemInfo = info;
        Exception = exception;
        Type = MonitorScanType.ScanSystem;
    }

    public MonitorScanMessage(MonitorAction action, MonitorEvents @event, IJobExecutionContext context, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(action);
        MonitorAction = action;
        Event = @event;
        JobExecutionContext = context;
        Exception = exception;
        Type = MonitorScanType.ExecuteJob;
    }

    public MonitorScanMessage(MonitorAction action, MonitorEvents @event, MonitorSystemInfo info, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(action);
        MonitorAction = action;
        Event = @event;
        MonitorSystemInfo = info;
        Exception = exception;
        Type = MonitorScanType.ExecuteSystem;
    }

    public MonitorScanType Type { get; private set; }
    public MonitorEvents Event { get; set; }
    public IJobExecutionContext? JobExecutionContext { get; set; }
    public Exception? Exception { get; set; }
    public MonitorSystemInfo? MonitorSystemInfo { get; set; }
    public MonitorAction? MonitorAction { get; set; }
    public bool ExecuteWithNoScan { get; set; }
}