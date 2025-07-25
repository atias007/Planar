using Planar.Service.General;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;
using TimeSpanConverter;

namespace Planar.Service.SystemJobs;

internal sealed class SchedulerHealthCheckJob(SchedulerHealthCheckUtil util) : SystemJob, IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        util.NotifyRun();
        return Task.CompletedTask;
    }

    public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
    {
        const string description = "System job for check health of current node scheduler";
        var cronexpr = TimeSpan.FromMinutes(1).ToCronExpression(); // Default health check interval for the current node scheduler
        await ScheduleHighPriority<SchedulerHealthCheckJob>(scheduler, description, cronexpr, stoppingToken: stoppingToken);
    }
}