using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Model;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs;

public sealed class ClearHistoryJob(IServiceScopeFactory serviceScopeFactory, ILogger<ClearHistoryJob> logger) : SystemJob, IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await SafeDoWork(context);
    }

    public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
    {
        const string description = "System job for clearing history records from database";
        var cronexpr = "0 30 0 ? * *";
        await ScheduleLowPriority<ClearHistoryJob>(scheduler, description, cronexpr, stoppingToken);
    }

    private async Task SafeDoWork(IJobExecutionContext context)
    {
        if (!await CheckIfStatisticsJobRun())
        {
            logger.LogWarning("could not clear history records, statistics job did not run today");
            return;
        }

        Task<IEnumerable<string>> ids;

        try
        {
            ids = GetExistsJobIds();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to get exists job ids at {Method}()", $"{nameof(ClearHistoryJob)}.{nameof(SafeDoWork)}");
            return;
        }

        await Task.WhenAll(
            ClearTrace(),
            ClearJobLog(),
            ClearJobWithRetentionDaysLog(),
            ClearStatistics(),
            ClearProperties(ids.Result),
            ClearLast(ids.Result),
            ClearHistory(ids.Result),
            ClearMonitorCountersByJob(ids.Result),
            ClearMonitorCountersByMonitor(),
            ClearJobStatistics(ids.Result)
            );

        SafeSetLastRun(context, logger);
    }

    private async Task<bool> CheckIfStatisticsJobRun()
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<IScheduler>();
            var job = await scheduler.GetJobDetail(new JobKey(nameof(StatisticsJob), Consts.PlanarSystemGroup));
            if (job == null || job.JobDataMap == null)
            {
                return false;
            }

            if (!job.JobDataMap.TryGetValue(LastRunKey, out var lastRun)) { return false; }
            var strLastRun = Convert.ToString(lastRun);
            if (string.IsNullOrWhiteSpace(strLastRun)) { return false; }
            if (!DateTime.TryParse(strLastRun, CultureInfo.CurrentCulture, out var lastRunTime)) { return false; }
            return lastRunTime.Date == DateTime.Now.Date;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to check if statistics job run today. skip clear history");
            return false;
        }
    }

    private async Task ClearStatistics()
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var data = scope.ServiceProvider.GetRequiredService<IMetricsData>();
            var rows = await data.ClearStatisticsTables(AppSettings.Retention.StatisticsRetentionDays);
            logger.LogDebug("clear statistics tables rows (older then {Days} days) with {Total} effected row(s)", AppSettings.Retention.StatisticsRetentionDays, rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to clear statistics tables rows (older then {Days} days)", AppSettings.Retention.StatisticsRetentionDays);
        }
    }

    private async Task ClearJobLog()
    {
        const int BatchSize = 5_000;
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var data = scope.ServiceProvider.GetRequiredService<IHistoryData>();
            var rows = 0;
            int count;
            do
            {
                count = await data.ClearJobLogTable(AppSettings.Retention.JobLogRetentionDays, BatchSize);
                rows += count;
            } while (count == BatchSize);

            logger.LogDebug("clear job log table rows (older then {Days} days) with {Total} effected row(s)", AppSettings.Retention.JobLogRetentionDays, rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to clear job log table rows (older then {Days} days)", AppSettings.Retention.JobLogRetentionDays);
        }
    }

    private async Task ClearJobWithRetentionDaysLog()
    {
        const int BatchSize = 5_000;
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<IScheduler>();
            var jobs = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            var data = scope.ServiceProvider.GetRequiredService<IHistoryData>();

            foreach (var item in jobs)
            {
                var job = await scheduler.GetJobDetail(item);
                if (job == null) { continue; }
                var days = JobHelper.GetLogRetentionDays(job);
                if (days == null) { continue; }
                var jobId = JobHelper.GetJobId(job);
                if (string.IsNullOrEmpty(jobId)) { continue; }

                int count;
                var rows = 0;
                do
                {
                    count = await data.ClearJobLogTable(jobId, days.Value, BatchSize);
                    rows += count;
                } while (count == BatchSize);

                logger.LogDebug("clear job {JobId} log table rows (older then {Days} days) with {Total} effected row(s)", jobId, days, rows);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to clear job log table rows (jobs with retention days)");
        }
    }

    private async Task ClearTrace()
    {
        const int BatchSize = 5_000;
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var data = scope.ServiceProvider.GetRequiredService<ITraceData>();
            int count;
            var rows = 0;
            do
            {
                count = await data.ClearTraceTable(AppSettings.Retention.TraceRetentionDays, BatchSize);
                rows += count;
            } while (count == BatchSize);

            logger.LogDebug("clear trace table rows (older then {Days} days) with {Total} effected row(s)", AppSettings.Retention.TraceRetentionDays, rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to clear trace table rows (older then {Days} days)", AppSettings.Retention.TraceRetentionDays);
        }
    }

    private async Task<IEnumerable<string>> GetExistsJobIds()
    {
        using var scope = serviceScopeFactory.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<IScheduler>();
        var existsKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        var filterKeys = existsKeys.Where(x => x.Group != Consts.PlanarSystemGroup).ToList();
        var jobDetails = filterKeys.Select(k => scheduler.GetJobDetail(k).Result);
        var existsIds = jobDetails
                .Where(d => d != null)
                .Select(d => JobKeyHelper.GetJobId(d) ?? string.Empty)
                .ToList();

        var result = existsIds.Where(i => !string.IsNullOrEmpty(i));
        return result;
    }

    private async Task ClearHistory(IEnumerable<string> existsIds)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var historyData = scope.ServiceProvider.GetRequiredService<IHistoryData>();
            var historyIds = await historyData.GetHistoryJobIds();
            var toBeDelete = historyIds.Except(existsIds);
            if (toBeDelete.Any())
            {
                await historyData.ClearJobHistory(toBeDelete);
            }

            logger.LogDebug("clear history table rows with {Total} effected row(s)", toBeDelete.Count());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to clear history table rows");
        }
    }

    private async Task ClearLast(IEnumerable<string> existsIds)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var historyData = scope.ServiceProvider.GetRequiredService<IHistoryData>();
            var lastIds = await historyData.GetLastHistoryJobIds();
            var toBeDelete = lastIds.Except(existsIds);
            if (toBeDelete.Any())
            {
                await historyData.ClearHistoryLastLogs(toBeDelete);
            }

            logger.LogDebug("clear history last table rows with {Total} effected row(s)", toBeDelete.Count());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to clear history last table rows");
        }
    }

    private async Task ClearProperties(IEnumerable<string> existsIds)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var jobData = scope.ServiceProvider.GetRequiredService<IJobData>();
            var ids = await jobData.GetJobPropertiesIds();
            var rows = 0;
            foreach (var id in ids)
            {
                if (!existsIds.Contains(id))
                {
                    await jobData.DeleteJobProperty(id);
                    logger.LogDebug("delete job property for job id {JobId}", id);
                    rows++;
                }
            }

            logger.LogDebug("clear properties table rows with {Total} effected row(s)", rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to clear properties table rows");
        }
    }

    private async Task ClearMonitorCountersByJob(IEnumerable<string> existsIds)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var data = scope.ServiceProvider.GetRequiredService<IMonitorData>();
            var ids = await data.GetMonitorCounterJobIds();
            var rows = 0;
            foreach (var id in ids)
            {
                if (!existsIds.Contains(id))
                {
                    await data.DeleteMonitorCounterByJobId(id);
                    logger.LogDebug("delete monitor counter for job id {JobId}", id);
                    rows++;
                }
            }

            logger.LogDebug("clear monitor counter rows with {Total} effected row(s)", rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to clear monitor counter rows");
        }
    }

    private async Task ClearMonitorCountersByMonitor()
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var data = scope.ServiceProvider.GetRequiredService<IMonitorData>();
            var ids = await data.GetMonitorCounterIds();
            var existsIds = await data.GetMonitorActionIds();
            var rows = 0;
            foreach (var id in ids)
            {
                if (!existsIds.Contains(id))
                {
                    await data.DeleteMonitorCounterByMonitorId(id);
                    logger.LogDebug("delete monitor counter for monitor id {MonitorId}", id);
                    rows++;
                }
            }

            logger.LogDebug("clear monitor counter rows with {Total} effected row(s)", rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to clear monitor counter rows");
        }
    }

    private async Task ClearJobStatistics(IEnumerable<string> existsIds)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var data = scope.ServiceProvider.GetRequiredService<IMetricsData>();
            var ids1 = await data.GetJobDurationStatisticsIds();

            var rows = 0;
            foreach (var id in ids1)
            {
                if (!existsIds.Contains(id))
                {
                    var stat = new JobDurationStatistic { JobId = id };
                    await data.DeleteJobStatistic(stat);
                    logger.LogDebug("delete job duration statistics for job id {JobId}", id);
                    rows++;
                }
            }

            var ids2 = await data.GetJobEffectedRowsStatisticsIds();
            foreach (var id in ids2)
            {
                if (!existsIds.Contains(id))
                {
                    var stat = new JobEffectedRowsStatistic { JobId = id };
                    await data.DeleteJobStatistic(stat);
                    logger.LogDebug("delete job effected rows statistics for job id {JobId}", id);
                    rows++;
                }
            }

            logger.LogDebug("clear statistics rows with {Total} effected row(s)", rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to clear statistics rows");
        }
    }
}