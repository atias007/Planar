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
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var span = TimeSpan.FromHours(24);
            var start = DateTime.Now.Date.AddDays(1).AddMinutes(10);
            await Schedule<StatisticsJob>(scheduler, description, span, start, stoppingToken);
        }

        private async Task SafeDoWork()
        {
            try
            {
                var data = _serviceProvider.GetRequiredService<StatisticsData>();
                var rows = await data.SetMaxConcurrentExecution();
                _logger.LogDebug("statistics job execute {Method} with {Total} effected row(s)", nameof(data.SetMaxConcurrentExecution), rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to set max concurrent execution");
            }

            try
            {
                var rows = await FillAnomaly();
                _logger.LogDebug("statistics job execute {Method} with {Total} effected row(s)", nameof(FillAnomaly), rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to fill anomaly execution");
            }

            try
            {
                var data = _serviceProvider.GetRequiredService<StatisticsData>();
                var rows = await data.FillJobCounters();
                _logger.LogDebug("statistics job execute {Method} with {Total} effected row(s)", nameof(StatisticsData.FillJobCounters), rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to fill job counter execution");
            }

            try
            {
                var data = _serviceProvider.GetRequiredService<StatisticsData>();
                var rows = await data.BuildJobStatistics();
                _logger.LogDebug("statistics job execute {Method} with {Total} effected row(s)", nameof(data.BuildJobStatistics), rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to build job statistics execution");
            }
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
                var data = _serviceProvider.GetRequiredService<StatisticsData>();
                statistics = await data.GetJobDurationStatistics();
                var cache = _serviceProvider.GetRequiredService<IMemoryCache>();
                cache.Set(nameof(StatisticsData.GetJobDurationStatistics), statistics, StatisticsUtil.DefaultCacheSpan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"fail to save job duration statistics cache while running system job: {nameof(StatisticsJob)}");
            }

            return statistics;
        }

        private async Task<IEnumerable<JobEffectedRowsStatistic>> SafeSetEffectedRowsStatisticsCache()
        {
            IEnumerable<JobEffectedRowsStatistic> statistics = new List<JobEffectedRowsStatistic>();

            try
            {
                var data = _serviceProvider.GetRequiredService<StatisticsData>();
                statistics = await data.GetJobEffectedRowsStatistics();
                var cache = _serviceProvider.GetRequiredService<IMemoryCache>();
                cache.Set(nameof(StatisticsData.GetJobEffectedRowsStatistics), statistics, StatisticsUtil.DefaultCacheSpan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"fail to save job duration statistics cache while running system job: {nameof(StatisticsJob)}");
            }

            return statistics;
        }

        private async Task<int> FillAnomaly()
        {
            var data = _serviceProvider.GetRequiredService<StatisticsData>();
            var logsQuery = data.GetNullAnomaly();
            var config = new MapperConfiguration(cfg => cfg.AddProfile<StatisticsProfile>());
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
}