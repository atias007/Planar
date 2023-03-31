using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.Data;
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
                var rows = await data.BuildJobStatistics();
                _logger.LogDebug("statistics job execute {Method} with {Total} effected row(s)", nameof(data.BuildJobStatistics), rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to build job statistics execution");
            }
        }

        private async Task<int> FillAnomaly()
        {
            var data = _serviceProvider.GetRequiredService<StatisticsData>();
            var logsQuery = data.GetNullAnomaly();
            var config = new MapperConfiguration(cfg => cfg.AddProfile<JobInstanceLogForStatisticsProfile>());
            var mapper = config.CreateMapper();
            var logs = await mapper.ProjectTo<JobInstanceLogForStatistics>(logsQuery).ToListAsync();
            var statistics = await data.GetJobStatistics();

            foreach (var item in logs)
            {
                if (item.Status == -1) { continue; }

                if (item.IsStopped || item.Status == 1)
                {
                    item.Anomaly = true;
                    continue;
                }

                var durationAnomaly = IsDurationAnomaly(item, statistics);
                if (durationAnomaly)
                {
                    item.Anomaly = true;
                    continue;
                }

                var effectedRowsAnomaly = IsEffectedRowsAnomaly(item, statistics);
                if (effectedRowsAnomaly)
                {
                    item.Anomaly = true;
                    continue;
                }

                item.Anomaly = false;
            }

            var notNullLogs = logs.Where(l => l.Anomaly != null).ToList();
            var logsToSave = mapper.Map<IEnumerable<JobInstanceLog>>(notNullLogs);
            data.SetAnomaly(logsToSave);
            await data.SaveChangesAsync();
            return logs.Count;
        }

        private static decimal GetZScore(decimal value, decimal avg, decimal stdev)
        {
            return (value - avg) / stdev;
        }

        private static decimal GetZScore(int value, decimal avg, decimal stdev)
        {
            var decValue = Convert.ToDecimal(value);
            return GetZScore(decValue, avg, stdev);
        }

        private static bool IsOutlier(decimal zscore, decimal lowerBound = -1.96M, decimal upperBound = 1.96M)
        {
            return zscore > upperBound || zscore < lowerBound;
        }

        private static bool IsDurationAnomaly(JobInstanceLogForStatistics? log, IEnumerable<JobStatistic> statistics)
        {
            if (log == null) { return false; }

            var stat = statistics.FirstOrDefault(j => j.JobId == log.JobId);
            if (stat == null) { return false; }

            var duration = log.Duration.GetValueOrDefault();
            var durationScore = GetZScore(duration, stat.AvgDuration, stat.StdevDuration);
            var durationAnomaly = IsOutlier(durationScore);
            return durationAnomaly;
        }

        private static bool IsEffectedRowsAnomaly(JobInstanceLogForStatistics? log, IEnumerable<JobStatistic> statistics)
        {
            if (log == null) { return false; }

            var stat = statistics.FirstOrDefault(j => j.JobId == log.JobId);
            if (stat == null) { return false; }
            if (stat.AvgEffectedRows == null) { return false; }
            if (stat.StdevEffectedRows == null) { return false; }
            if (stat.StdevEffectedRows == 0) { return false; }

            var effectedRows = log.EffectedRows.GetValueOrDefault();
            var effectedRowsScore = GetZScore(effectedRows, stat.AvgEffectedRows.Value, stat.StdevEffectedRows.Value);
            var durationAnomaly = IsOutlier(effectedRowsScore);
            return durationAnomaly;
        }
    }
}