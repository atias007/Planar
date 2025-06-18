using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.Data;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs;

internal class MonitorJob(IServiceScopeFactory scopeFactory, ILogger<MonitorJob> logger) : SystemJob, IJob
{
    private readonly ILogger<MonitorJob> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private static readonly int _periodMinutes = Convert.ToInt32(AppSettings.Monitor.MaxAlertsPeriod.TotalMinutes);

    public async Task Execute(IJobExecutionContext context)
    {
        bool success = true;
        try
        {
            await ResetMonitorCounter();
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError(ex, "fail to reset monitor counter: {Message}", ex.Message);
        }

        try
        {
            await DeleteOldMonitorMutes();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to delete old monitor mutes: {Message}", ex.Message);
        }

        if (success)
        {
            SafeSetLastRun(context, _logger);
        }
    }

    public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
    {
        const string description = "system job for reset monirors with mute status";
        var span = TimeSpan.FromMinutes(15);
        await ScheduleHighPriority<MonitorJob>(scheduler, description, span, stoppingToken: stoppingToken);
    }

    private async Task ResetMonitorCounter()
    {
        using var scope = _scopeFactory.CreateScope();
        var dal = scope.ServiceProvider.GetRequiredService<IMonitorData>();
        await dal.ResetMonitorCounter(_periodMinutes);
    }

    private async Task DeleteOldMonitorMutes()
    {
        using var scope = _scopeFactory.CreateScope();
        var dal = scope.ServiceProvider.GetRequiredService<IMonitorData>();
        await dal.DeleteOldMonitorMutes();
    }
}