using System;

namespace Planar.Service.General;

public sealed class SchedulerHealthCheckUtil
{
    public DateTimeOffset LastRun { get; private set; } = DateTimeOffset.UtcNow;

    public void NotifyRun()
    {
        LastRun = DateTimeOffset.UtcNow;
    }
}