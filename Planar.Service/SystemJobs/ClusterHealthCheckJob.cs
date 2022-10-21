using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.General;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs
{
    public class ClusterHealthCheckJob : BaseSystemJob, IJob
    {
        private readonly ILogger<ClusterHealthCheckJob> _logger;

        public ClusterHealthCheckJob()
        {
            _logger = Global.GetLogger<ClusterHealthCheckJob>();
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                return DoWork();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail check health of cluster: {Message}", ex.Message);
                return Task.CompletedTask;
            }
        }

        private static async Task DoWork()
        {
            var logger = Global.GetLogger<ClusterHealthCheckJob>();
            var dal = Global.ServiceProvider.GetService<DataLayer>();
            var util = new ClusterUtil(dal, logger);

            if (AppSettings.Clustering)
            {
                await util.HealthCheckWithUpdate();
            }
        }

        public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
        {
            const string description = "System job for check health of cluster nodes";
            var span = AppSettings.ClusterHealthCheckInterval;
            var jobKey = await Schedule<ClusterHealthCheckJob>(scheduler, description, span, stoppingToken: stoppingToken);

            if (AppSettings.Clustering == false)
            {
                await scheduler.PauseJob(jobKey);
            }
        }
    }
}