using Planar.Common;
using System;
using System.Threading.Tasks;

namespace Planar.Service.General;

internal sealed class SchedulerHealthCheckUtil(SchedulerUtil schedulerUtil)
{
    public DateTimeOffset LastRun { get; private set; } = DateTimeOffset.UtcNow;

    public void NotifyRun()
    {
        LastRun = DateTimeOffset.UtcNow;
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            // Check if the scheduler is started and running
            if (!schedulerUtil.IsSchedulerRunning) { return false; }

            // Check if the last run was within a reasonable time frame (e.g., 5 minutes)
            var timeSinceLastRun = DateTimeOffset.UtcNow - LastRun;
            if (timeSinceLastRun.TotalMinutes < 5) { return true; }

            var running = await schedulerUtil.CountRunningJobs();
            var maxConcurrency = AppSettings.General.MaxConcurrency;
            if (running < maxConcurrency) { return false; }
            return true;
        }
        catch
        {
            // If any exception occurs, consider the scheduler unhealthy
            return false;
        }
    }
}