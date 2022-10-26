using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.Data;
using Planar.Service.General;
using Planar.Service.Model.DataObjects;
using Polly;
using Quartz;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DbJobInstanceLog = Planar.Service.Model.JobInstanceLog;

namespace Planar.Service.SystemJobs
{
    public class PersistDataJob : BaseSystemJob, IJob
    {
        private readonly ILogger<PersistDataJob> _logger;

        private readonly DataLayer _dal;
        private readonly ClusterUtil _clusterUtil;
        private readonly SchedulerUtil _schedulerUtil;

        public PersistDataJob(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<PersistDataJob>>();
            _dal = serviceProvider.GetService<DataLayer>();
            _clusterUtil = serviceProvider.GetService<ClusterUtil>();
            _schedulerUtil = serviceProvider.GetService<SchedulerUtil>();
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                return DoWork();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to persist data: {Message}", ex.Message);
                return Task.CompletedTask;
            }
        }

        public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
        {
            const string description = "System job for persist log & exception from running jobs";
            var span = AppSettings.PersistRunningJobsSpan;
            await Schedule<PersistDataJob>(scheduler, description, span, stoppingToken: stoppingToken);
        }

        private async Task DoWork()
        {
            var runningJobs = await _schedulerUtil.GetPersistanceRunningJobsInfo();

            if (AppSettings.Clustering)
            {
                var clusterRunningJobs = await _clusterUtil.GetPersistanceRunningJobsInfo();
                runningJobs ??= new List<PersistanceRunningJobsInfo>();

                if (clusterRunningJobs != null)
                {
                    runningJobs.AddRange(clusterRunningJobs);
                }
            }

            foreach (var context in runningJobs)
            {
                var log = new DbJobInstanceLog
                {
                    InstanceId = context.InstanceId,
                    Log = context.Log,
                    Exception = context.Exceptions,
                    Duration = context.Duration,
                };

                _logger.LogInformation("Persist data for job {Group}.{Name}", context.Group, context.Name);
                await Policy.Handle<Exception>()
                        .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(1 * i))
                        .ExecuteAsync(() => _dal.PersistJobInstanceData(log));
            }
        }
    }
}