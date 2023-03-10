using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs
{
    public class ClusterHealthCheckJob : SystemJob, IJob
    {
        private readonly ILogger<ClusterHealthCheckJob> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ClusterHealthCheckJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetRequiredService<ILogger<ClusterHealthCheckJob>>();
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                return DoWork();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fail check health of cluster: {Message}", ex.Message);
                return Task.CompletedTask;
            }
        }

        private async Task DoWork()
        {
            if (AppSettings.Clustering)
            {
                var util = _serviceProvider.GetRequiredService<ClusterUtil>();
                await util.HealthCheckWithUpdate();
            }
        }

        public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
        {
            const string description = "System job for check health of cluster nodes";
            var span = AppSettings.ClusterHealthCheckInterval;
            var jobKey = await Schedule<ClusterHealthCheckJob>(scheduler, description, span, stoppingToken: stoppingToken);

            if (!AppSettings.Clustering)
            {
                await scheduler.PauseJob(jobKey, stoppingToken);
            }
        }
    }
}