using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;
using TimeSpanConverter;

namespace Planar.Service.SystemJobs;

public sealed class ClusterHealthCheckJob : SystemJob, IJob
{
    private readonly ILogger<ClusterHealthCheckJob> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMonitorUtil _monitorUtil;

    public ClusterHealthCheckJob(IServiceProvider serviceProvider, IMonitorUtil monitorUtil)
    {
        _monitorUtil = monitorUtil;
        _serviceProvider = serviceProvider;
        _logger = _serviceProvider.GetRequiredService<ILogger<ClusterHealthCheckJob>>();
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await SafeDoWork(context);
    }

    private async Task SafeDoWork(IJobExecutionContext context)
    {
        try
        {
            await DoWork();
            SafeSetLastRun(context, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail check health of cluster: {Message}", ex.Message);
            _monitorUtil.Scan(MonitorEvents.ClusterHealthCheckFail, context, ex);
        }
    }

    private async Task DoWork()
    {
        if (AppSettings.Cluster.Clustering)
        {
            var util = _serviceProvider.GetRequiredService<ClusterUtil>();
            await util.HealthCheckWithUpdate();
        }
    }

    public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
    {
        const string description = "System job for check health of cluster nodes";
        var cronexpr = AppSettings.Cluster.HealthCheckInterval.ToCronExpression();
        var jobKey = await ScheduleHighPriority<ClusterHealthCheckJob>(scheduler, description, cronexpr, stoppingToken: stoppingToken);

        if (AppSettings.Cluster.Clustering)
        {
            await scheduler.ResumeJob(jobKey, stoppingToken);
        }
        else
        {
            await scheduler.PauseJob(jobKey, stoppingToken);
        }
    }
}