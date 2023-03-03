using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.Data;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs
{
    public class StatisticsJob : BaseSystemJob, IJob
    {
        private readonly ILogger<StatisticsJob> _logger;

        private readonly StatisticsData _statisticsData;

        public StatisticsJob(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<StatisticsJob>>();
            _statisticsData = serviceProvider.GetRequiredService<StatisticsData>();
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
            await Schedule<ClearTraceTableJob>(scheduler, description, span, start, stoppingToken);
        }

        private async Task SafeDoWork()
        {
            try
            {
                await _statisticsData.SetMaxConcurentExecution();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to set max concurent execution");
            }

            try
            {
                await _statisticsData.SetMaxDurationExecution();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to set max duration execution");
            }
        }
    }
}