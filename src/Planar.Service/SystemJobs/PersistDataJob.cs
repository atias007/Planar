using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.General;
using Planar.Service.Model.DataObjects;
using Polly;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DbJobInstanceLog = Planar.Service.Model.JobInstanceLog;

namespace Planar.Service.SystemJobs
{
    public sealed class PersistDataJob : SystemJob, IJob
    {
        private readonly ILogger<PersistDataJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public PersistDataJob(IServiceScopeFactory scopeFactory, ILogger<PersistDataJob> logger)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                return DoWork();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fail to persist data: {Message}", ex.Message);
                return Task.CompletedTask;
            }
        }

        public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
        {
            const string description = "System job for persist log & exception from running jobs";
            var span = AppSettings.General.PersistRunningJobsSpan;
            await Schedule<PersistDataJob>(scheduler, description, span, stoppingToken: stoppingToken);
        }

        private async Task DoWork()
        {
            using var scope = _scopeFactory.CreateScope();
            var schedulerUtil = scope.ServiceProvider.GetRequiredService<SchedulerUtil>();
            var runningJobs = await schedulerUtil.GetPersistanceRunningJobsInfo();

            if (AppSettings.Cluster.Clustering)
            {
                var clusterUtil = scope.ServiceProvider.GetRequiredService<ClusterUtil>();
                var clusterRunningJobs = await clusterUtil.GetPersistanceRunningJobsInfo();
                runningJobs ??= new List<PersistanceRunningJobsInfo>();

                if (clusterRunningJobs != null)
                {
                    runningJobs.AddRange(clusterRunningJobs);
                }
            }

            if (!runningJobs.Any()) { return; }

            foreach (var context in runningJobs)
            {
                var log = new DbJobInstanceLog
                {
                    InstanceId = context.InstanceId ?? Consts.Undefined,
                    Log = context.Log,
                    Exception = context.Exceptions,
                    Duration = context.Duration,
                };

                var dal = scope.ServiceProvider.GetService<HistoryData>();
                await Policy.Handle<Exception>()
                        .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(1 * i))
                        .ExecuteAsync(() => dal?.PersistJobInstanceData(log));
            }
        }
    }
}