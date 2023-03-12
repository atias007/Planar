using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.Data;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs
{
    public class StatisticsJob : SystemJob, IJob
    {
        private readonly ILogger<StatisticsJob> _logger;
        private readonly IServiceProvider _serviceProvider;

        public StatisticsJob(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<StatisticsJob>>();
            _serviceProvider = serviceProvider;
        }

        public Task Execute(IJobExecutionContext context)
        {
            return SafeDoWork();
        }

        public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
        {
            const string description = "System job for saving statistics data to database";
            var span = TimeSpan.FromHours(1);
            var start = DateTime.Now.Date.AddMinutes(1);
            await Schedule<StatisticsJob>(scheduler, description, span, start, stoppingToken);
        }

        private async Task SafeDoWork()
        {
            try
            {
                var data = _serviceProvider.GetRequiredService<StatisticsData>();
                var rows = await data.SetMaxConcurentExecution();
                _logger.LogDebug("statistics job execute {Method} with {Total} effected row(s)", nameof(data.SetMaxConcurentExecution), rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to set max concurent execution");
            }

            try
            {
                var data = _serviceProvider.GetRequiredService<StatisticsData>();
                var rows = await data.SetMaxDurationExecution();
                _logger.LogDebug("statistics job execute {Method} with {Total} effected row(s)", nameof(data.SetMaxDurationExecution), rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to set max duration execution");
            }
        }
    }
}