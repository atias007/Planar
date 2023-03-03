using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.Data;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs
{
    public class ClearTraceTableJob : BaseSystemJob, IJob
    {
        private readonly ILogger<ClearTraceTableJob> _logger;

        private readonly TraceData _traceData;

        private readonly StatisticsData _statisticsData;

        public ClearTraceTableJob(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<ClearTraceTableJob>>();
            _traceData = serviceProvider.GetRequiredService<TraceData>();
            _statisticsData = serviceProvider.GetRequiredService<StatisticsData>();
        }

        public Task Execute(IJobExecutionContext context)
        {
            return SafeDoWork();
        }

        public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
        {
            const string description = "System job for clearing old trace records from database";
            var span = TimeSpan.FromHours(24);
            var start = DateTime.Now.Date.AddDays(1);
            await Schedule<ClearTraceTableJob>(scheduler, description, span, start, stoppingToken);
        }

        private async Task SafeDoWork()
        {
            try
            {
                await _traceData?.ClearTraceTable(AppSettings.ClearTraceTableOverDays);
                _logger.LogInformation("Clear trace table rows (older then {Days} days)", AppSettings.ClearTraceTableOverDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to clear trace table rows (older then {Days} days)", AppSettings.ClearTraceTableOverDays);
            }

            try
            {
                await _statisticsData?.ClearStatisticsTables(AppSettings.ClearTraceTableOverDays);
                _logger.LogInformation("Clear statistics table rows (older then {Days} days)", AppSettings.ClearTraceTableOverDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to clear statistics table rows (older then {Days} days)", AppSettings.ClearTraceTableOverDays);
            }
        }
    }
}