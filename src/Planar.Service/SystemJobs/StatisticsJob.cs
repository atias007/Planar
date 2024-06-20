using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.Data;
using Planar.Service.General;
using Planar.Service.MapperProfiles;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
using Polly;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs;

public sealed class StatisticsJob : SystemJob, IJob
{
    private readonly ILogger<StatisticsJob> _logger;
    private readonly IServiceScopeFactory _serviceScope;

    public StatisticsJob(IServiceScopeFactory serviceScope, ILogger<StatisticsJob> logger)
    {
        _logger = logger;
        _serviceScope = serviceScope;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await SafeDoWork(context);
    }

    public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
    {
        const string description = "System job for saving statistics data to database";
        var span = TimeSpan.FromHours(24);
        var start = DateTime.Now.Date.AddDays(1).AddMinutes(5);
        await Schedule<StatisticsJob>(scheduler, description, span, start, stoppingToken);
    }

    private async Task SafeDoWork(IJobExecutionContext context)
    {
        using var scope = _serviceScope.CreateScope();
        try
        {
            var data = scope.ServiceProvider.GetRequiredService<MetricsData>();
            var rows = await data.SetMaxConcurrentExecution();
            _logger.LogDebug("statistics job execute {Method} with {Total} effected row(s)", nameof(data.SetMaxConcurrentExecution), rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to set max concurrent execution");
        }

        try
        {
            var rows = await FillAnomaly(scope.ServiceProvider);
            _logger.LogDebug("statistics job execute {Method} with {Total} effected row(s)", nameof(FillAnomaly), rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to fill anomaly execution");
        }

        try
        {
            var data = scope.ServiceProvider.GetRequiredService<MetricsData>();
            var rows = await data.FillJobCounters();
            _logger.LogDebug("statistics job execute {Method} with {Total} effected row(s)", nameof(MetricsData.FillJobCounters), rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to fill job counter execution");
        }

        try
        {
            var data = scope.ServiceProvider.GetRequiredService<MetricsData>();
            var rows = await data.BuildJobStatistics();
            _logger.LogDebug("statistics job execute {Method} with {Total} effected row(s)", nameof(data.BuildJobStatistics), rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to build job statistics execution");
        }

        SafeSetLastRun(context, _logger);
    }

    private async Task<JobStatistics> SafeSetStatisticsCache()
    {
        var task1 = SafeSetDurationStatisticsCache();
        var task2 = SafeSetEffectedRowsStatisticsCache();
        var result = new JobStatistics
        {
            JobDurationStatistics = await task1,
            JobEffectedRowsStatistic = await task2,
        };

        return result;
    }

    private async Task<IEnumerable<JobDurationStatistic>> SafeSetDurationStatisticsCache()
    {
        IEnumerable<JobDurationStatistic> statistics = new List<JobDurationStatistic>();
        try
        {
            using var scope = _serviceScope.CreateScope();
            var data = scope.ServiceProvider.GetRequiredService<MetricsData>();
            statistics = await data.GetJobDurationStatistics();
            var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
            cache.Set(nameof(MetricsData.GetJobDurationStatistics), statistics, StatisticsUtil.DefaultCacheSpan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to save job duration statistics cache while running system job: {Name}", nameof(StatisticsJob));
        }

        return statistics;
    }

    private async Task<IEnumerable<JobEffectedRowsStatistic>> SafeSetEffectedRowsStatisticsCache()
    {
        IEnumerable<JobEffectedRowsStatistic> statistics = new List<JobEffectedRowsStatistic>();

        try
        {
            using var scope = _serviceScope.CreateScope();
            var data = scope.ServiceProvider.GetRequiredService<MetricsData>();
            statistics = await data.GetJobEffectedRowsStatistics();
            var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
            cache.Set(nameof(MetricsData.GetJobEffectedRowsStatistics), statistics, StatisticsUtil.DefaultCacheSpan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to save job duration statistics cache while running system job: {Name}", nameof(StatisticsJob));
        }

        return statistics;
    }

    private async Task<int> FillAnomaly(IServiceProvider serviceProvider)
    {
        var data = serviceProvider.GetRequiredService<MetricsData>();
        var logsQuery = data.GetNullAnomaly();
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MetricsProfile>());
        var mapper = config.CreateMapper();
        var logs = await mapper.ProjectTo<JobInstanceLogForStatistics>(logsQuery).ToListAsync();
        var statistics = await SafeSetStatisticsCache();

        foreach (var item in logs)
        {
            StatisticsUtil.SetAnomaly(item, statistics);
        }

        var notNullLogs = logs.Where(l => l.Anomaly != null).ToList();
        var logsToSave = mapper.Map<IEnumerable<JobInstanceLog>>(notNullLogs)
            .Where(l => l.Anomaly != null);

        data.SetAnomaly(logsToSave);
        await data.SaveChangesAsync();
        return logs.Count;
    }
}