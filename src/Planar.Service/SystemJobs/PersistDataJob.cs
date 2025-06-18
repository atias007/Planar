using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.General;
using Polly;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;
using DbJobInstanceLog = Planar.Service.Model.JobInstanceLog;

namespace Planar.Service.SystemJobs;

public sealed class PersistDataJob(IServiceScopeFactory scopeFactory, ILogger<PersistDataJob> logger) : SystemJob, IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await SafeDoWork(context);
    }

    public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
    {
        const string description = "system job for persist log & exception from running jobs";
        var span = AppSettings.General.PersistRunningJobsSpan;
        await ScheduleHighPriority<PersistDataJob>(scheduler, description, span, stoppingToken: stoppingToken);
    }

    private async Task SafeDoWork(IJobExecutionContext context)
    {
        try
        {
            await DoWork();
            SafeSetLastRun(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fail to persist data: {Message}", ex.Message);
        }
    }

    private async Task DoWork()
    {
        using var scope = scopeFactory.CreateScope();
        var schedulerUtil = scope.ServiceProvider.GetRequiredService<SchedulerUtil>();
        var runningJobs = await schedulerUtil.GetPersistanceRunningJobsInfo();

        if (AppSettings.Cluster.Clustering)
        {
            var clusterUtil = scope.ServiceProvider.GetRequiredService<ClusterUtil>();
            var clusterRunningJobs = await clusterUtil.GetPersistanceRunningJobsInfo();
            runningJobs ??= [];

            if (clusterRunningJobs != null)
            {
                runningJobs.AddRange(clusterRunningJobs);
            }
        }

        if (runningJobs.Count == 0) { return; }

        foreach (var context in runningJobs)
        {
            var log = new DbJobInstanceLog
            {
                InstanceId = context.InstanceId ?? Consts.Undefined,
                Log = context.Log,
                Exception = context.Exceptions,
                Duration = context.Duration,
            };

            var dal = scope.ServiceProvider.GetRequiredService<IHistoryData>();
            await Policy.Handle<Exception>()
                    .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(1 * i))
                    .ExecuteAsync(() => dal?.PersistJobInstanceData(log));
        }
    }
}