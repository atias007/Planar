using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.Data;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs
{
    internal class MonitorJob : SystemJob, IJob
    {
        private readonly ILogger<MonitorJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private static readonly int _periodMinutes = Convert.ToInt32(AppSettings.Monitor.MaxAlertsPeriod.TotalMinutes);

        public MonitorJob(IServiceScopeFactory scopeFactory, ILogger<MonitorJob> logger)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                return ResetMonitorMute();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fail to reset monitor mute: {Message}", ex.Message);
                return Task.CompletedTask;
            }
        }

        public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
        {
            const string description = "system job for reset monirors with mute status";
            var span = TimeSpan.FromMinutes(5);
            await Schedule<MonitorJob>(scheduler, description, span, stoppingToken: stoppingToken);
        }

        private async Task ResetMonitorMute()
        {
            using var scope = _scopeFactory.CreateScope();
            var dal = scope.ServiceProvider.GetRequiredService<MonitorData>();
            await dal.ResetMonitorCounter(_periodMinutes);
        }
    }
}