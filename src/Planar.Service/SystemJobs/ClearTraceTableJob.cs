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

        private readonly TraceData _dal;

        public ClearTraceTableJob(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<ClearTraceTableJob>>();
            _dal = serviceProvider.GetRequiredService<TraceData>();
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                return DoWork();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to clear trace table: {Message}", ex.Message);
                return Task.CompletedTask;
            }
        }

        public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
        {
            const string description = "System job for clearing old trace records from database";
            var span = TimeSpan.FromHours(24);
            var start = DateTime.Now.Date.AddDays(1);
            await Schedule<ClearTraceTableJob>(scheduler, description, span, start, stoppingToken);
        }

        private async Task DoWork()
        {
            await _dal.ClearTraceTable(AppSettings.ClearTraceTableOverDays);
            _logger.LogInformation("Clear trace table rows (older then {Days} days)", AppSettings.ClearTraceTableOverDays);
        }
    }
}