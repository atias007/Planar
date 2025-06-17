using Planar.Service.General;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs;

internal sealed class ServiceHealthCheckJob(SchedulerHealthCheckUtil util) : SystemJob, IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        util.NotifyRun();
        return Task.CompletedTask;
    }

    public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
    {
        const string description = "System job for check health of current node scheduler";
        var span = TimeSpan.FromMinutes(1); // Default health check interval for the current node scheduler
        await Schedule<ServiceHealthCheckJob>(scheduler, description, span, stoppingToken: stoppingToken);
    }
}