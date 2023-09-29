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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs
{
    public class ClearHistoryJob : SystemJob, IJob
    {
        private readonly ILogger<ClearHistoryJob> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ClearHistoryJob(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<ClearHistoryJob>>();
            _serviceProvider = serviceProvider;
        }

        public Task Execute(IJobExecutionContext context)
        {
            return SafeDoWork();
        }

        public static async Task Schedule(IScheduler scheduler, CancellationToken stoppingToken = default)
        {
            const string description = "System job for clearing history records from database";
            var span = TimeSpan.FromHours(24);
            var start = DateTime.Now.Date.AddDays(1).AddMinutes(5);
            await Schedule<ClearHistoryJob>(scheduler, description, span, start, stoppingToken);
        }

        private async Task SafeDoWork()
        {
            var ids = GetExistsJobIds();

            await Task.WhenAll(
                ClearTrace(),
                ClearJobLog(),
                ClearJobWithRetentionDaysLog(),
                ClearStatistics(),
                ClearProperties(ids.Result),
                ClearJobStatistics(ids.Result)
                );
        }

        private async Task ClearStatistics()
        {
            try
            {
                var data = _serviceProvider.GetRequiredService<MetricsData>();
                var rows = await data.ClearStatisticsTables(AppSettings.Retention.StatisticsRetentionDays);
                _logger.LogDebug("clear statistics tables rows (older then {Days} days) with {Total} effected row(s)", AppSettings.Retention.StatisticsRetentionDays, rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "fail to clear statistics tables rows (older then {Days} days)", AppSettings.Retention.StatisticsRetentionDays);
            }
        }

        private async Task ClearJobLog()
        {
            try
            {
                var data = _serviceProvider.GetRequiredService<HistoryData>();
                var rows = await data.ClearJobLogTable(AppSettings.Retention.JobLogRetentionDays);
                _logger.LogDebug("clear job log table rows (older then {Days} days) with {Total} effected row(s)", AppSettings.Retention.JobLogRetentionDays, rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "fail to clear job log table rows (older then {Days} days)", AppSettings.Retention.JobLogRetentionDays);
            }
        }

        private async Task ClearJobWithRetentionDaysLog()
        {
            try
            {
                var scheduler = _serviceProvider.GetRequiredService<IScheduler>();
                var jobs = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
                var data = _serviceProvider.GetRequiredService<HistoryData>();

                foreach (var item in jobs)
                {
                    var job = await scheduler.GetJobDetail(item);
                    if (job == null) { continue; }
                    var days = JobHelper.GetLogRetentionDays(job);
                    if (days == null) { continue; }
                    var jobId = JobHelper.GetJobId(job);
                    if (string.IsNullOrEmpty(jobId)) { continue; }
                    var rows = await data.ClearJobLogTable(jobId, days.Value);
                    _logger.LogDebug("clear job {JobId} log table rows (older then {Days} days) with {Total} effected row(s)", jobId, days, rows);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "fail to clear job log table rows (jobs with retention days)");
            }
        }

        private async Task ClearTrace()
        {
            try
            {
                var data = _serviceProvider.GetRequiredService<TraceData>();
                var rows = await data.ClearTraceTable(AppSettings.Retention.TraceRetentionDays);
                _logger.LogDebug("clear trace table rows (older then {Days} days) with {Total} effected row(s)", AppSettings.Retention.TraceRetentionDays, rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "fail to clear trace table rows (older then {Days} days)", AppSettings.Retention.TraceRetentionDays);
            }
        }

        private async Task<IEnumerable<string>> GetExistsJobIds()
        {
            var scheduler = _serviceProvider.GetRequiredService<IScheduler>();
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

        private async Task ClearProperties(IEnumerable<string> existsIds)
        {
            try
            {
                var data = _serviceProvider.GetRequiredService<JobData>();
                var ids = await data.GetJobPropertiesIds();
                var rows = 0;
                foreach (var id in ids)
                {
                    if (!existsIds.Contains(id))
                    {
                        await data.DeleteJobProperty(id);
                        _logger.LogDebug("delete job property for job id {JobId}", id);
                        rows++;
                    }
                }

                _logger.LogDebug("clear properties table rows with {Total} effected row(s)", rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to clear properties table rows");
            }
        }

        private async Task ClearJobStatistics(IEnumerable<string> existsIds)
        {
            try
            {
                var data = _serviceProvider.GetRequiredService<MetricsData>();
                var ids1 = await data.GetJobDurationStatisticsIds();

                var rows = 0;
                foreach (var id in ids1)
                {
                    if (!existsIds.Contains(id))
                    {
                        var stat = new JobDurationStatistic { JobId = id };
                        await data.DeleteJobStatistic(stat);
                        _logger.LogDebug("delete job duration statistics for job id {JobId}", id);
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
                        _logger.LogDebug("delete job effected rows statistics for job id {JobId}", id);
                        rows++;
                    }
                }

                _logger.LogDebug("clear statistics rows with {Total} effected row(s)", rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to clear statistics rows");
            }
        }
    }
}