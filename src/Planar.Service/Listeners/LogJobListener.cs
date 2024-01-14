using CommonJob;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.General;
using Planar.Service.Listeners.Base;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DbJobInstanceLog = Planar.Service.Model.JobInstanceLog;

namespace Planar.Service.Listeners
{
    public class LogJobListener : BaseListener<LogJobListener>, IJobListener
    {
        public LogJobListener(IServiceScopeFactory serviceScopeFactory, ILogger<LogJobListener> logger) : base(serviceScopeFactory, logger)
        {
        }

        public string Name => nameof(LogJobListener);

        public async Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (IsSystemJob(context.JobDetail)) { return; }
                await ExecuteDal<HistoryData>(d => d.SetJobInstanceLogStatus(context.FireInstanceId, StatusMembers.Veto));
            }
            catch (Exception ex)
            {
                LogCritical(nameof(JobExecutionVetoed), ex);
            }
            finally
            {
                SafeScan(MonitorEvents.ExecutionVetoed, context, null);
            }
        }

        public async Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (IsSystemJob(context.JobDetail)) { return; }
                var statisticsTask = AddConcurrentStatistics(context);
                var data = GetJobDataForLogging(context.MergedJobDataMap);

                var log = new DbJobInstanceLog
                {
                    InstanceId = context.FireInstanceId,
                    Data = data,
                    StartDate = context.FireTimeUtc.ToLocalTime().DateTime,
                    Status = (int)StatusMembers.Running,
                    StatusTitle = StatusMembers.Running.ToString(),
                    JobId = JobKeyHelper.GetJobId(context.JobDetail) ?? string.Empty,
                    JobName = context.JobDetail.Key.Name,
                    JobGroup = context.JobDetail.Key.Group,
                    JobType = SchedulerUtil.GetJobTypeName(context.JobDetail.JobType),
                    TriggerId = TriggerHelper.GetTriggerId(context.Trigger) ?? Consts.ManualTriggerId,
                    TriggerName = context.Trigger.Key.Name,
                    TriggerGroup = context.Trigger.Key.Group,
                    Retry = context.Trigger.Key.Group == Consts.RetryTriggerGroup,
                    ServerName = Environment.MachineName
                };

                log.Data?.Trim();

                if (log.InstanceId.Length > 250) { log.InstanceId = log.InstanceId[0..250]; }
                if (log.Data?.Length > 4000) { log.Data = log.Data[0..4000]; }
                if (log.JobId?.Length > 20) { log.JobId = log.JobId[0..20]; }
                if (log.JobName.Length > 50) { log.JobName = log.JobName[0..50]; }
                if (log.JobGroup.Length > 50) { log.JobGroup = log.JobGroup[0..50]; }
                if (log.JobType.Length > 50) { log.JobType = log.JobType[0..50]; }
                if (log.TriggerId.Length > 20) { log.TriggerId = log.TriggerId[0..20]; }
                if (log.TriggerName.Length > 50) { log.TriggerName = log.TriggerName[0..50]; }
                if (log.TriggerGroup.Length > 50) { log.TriggerGroup = log.TriggerGroup[0..50]; }
                if (log.ServerName.Length > 50) { log.ServerName = log.ServerName[0..50]; }

                await ExecuteDal<HistoryData>(d => d.CreateJobInstanceLog(log));
                await statisticsTask;
            }
            catch (Exception ex)
            {
                LogCritical(nameof(JobToBeExecuted), ex);
            }
            finally
            {
                SafeScan(MonitorEvents.ExecutionStart, context, null);
            }
        }

        public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken cancellationToken = default)
        {
            Exception? executionException = null;

            try
            {
                if (IsSystemJob(context.JobDetail)) { return; }

                var unhadleException = JobExecutionMetadata.GetInstance(context)?.UnhandleException;
                executionException = unhadleException ?? jobException;

                var duration = context.JobRunTime.TotalMilliseconds;
                var endDate = context.FireTimeUtc.ToLocalTime().DateTime.Add(context.JobRunTime);
                var status = executionException == null ? StatusMembers.Success : StatusMembers.Fail;

                var metadata = context.Result as JobExecutionMetadata;

                var log = new DbJobInstanceLog
                {
                    JobId = JobKeyHelper.GetJobId(context.JobDetail) ?? string.Empty, // for function: SafeFillAnomaly(log);
                    InstanceId = context.FireInstanceId,
                    Duration = Convert.ToInt32(duration),
                    EndDate = endDate,
                    Exception = GetExceptionText(executionException),
                    ExceptionCount = metadata?.Exceptions.Count ?? 0,
                    EffectedRows = metadata?.EffectedRows,
                    Log = metadata?.Log.ToString(),
                    Status = (int)status,
                    StatusTitle = status.ToString(),
                    IsCanceled = context.CancellationToken.IsCancellationRequested
                };

                log.Log?.Trim();

                if (log.StatusTitle.Length > 10) { log.StatusTitle = log.StatusTitle[0..10]; }

                await ExecuteDal<HistoryData>(d => d.UpdateHistoryJobRunLog(log));
                await SafeFillAnomaly(log);
            }
            catch (Exception ex)
            {
                LogCritical(nameof(JobWasExecuted), ex);
            }
            finally
            {
                SafeMonitorJobWasExecuted(context, executionException);
            }
        }

        private static async Task<int> CountConcurrentExecutionJob(IScheduler scheduler)
        {
            var first = await scheduler.GetCurrentlyExecutingJobs();
            var second = first.Select(f => f.JobDetail.Key)
                .Count(f => !IsSystemJobKey(f));

            return second;
        }

        private static string GetAllExceptionMessages(Exception ex)
        {
            var messages = new StringBuilder(ex.Message);

            // Traverse inner exceptions using a loop
            var innerException = ex.InnerException;
            while (innerException != null)
            {
                messages.AppendLine(innerException.Message);
                innerException = innerException.InnerException;
            }

            return messages.ToString();
        }

        private static string? GetExceptionText(Exception? ex)
        {
            if (ex == null) { return null; }
            if (ex is PlanarJobExecutionException jobEx)
            {
                return jobEx.ExceptionText;
            }
            else
            {
                var result = $"{ex.GetType().FullName}: {GetAllExceptionMessages(ex)}";
                return result;
            }
        }

        private static string? GetJobDataForLogging(JobDataMap data)
        {
            if (data?.Count == 0) { return null; }

            var items = Global.ConvertDataMapToDictionary(data);
            if (items?.Count == 0) { return null; }

            var yml = YmlUtil.Serialize(items);
            return yml?.Trim();
        }

        private async Task AddConcurrentStatistics(IJobExecutionContext context)
        {
            var count = await CountConcurrentExecutionJob(context.Scheduler);
            var item = new ConcurrentQueue
            {
                ConcurrentValue = Convert.ToInt16(count + 1),
                Server = Environment.MachineName,
                InstanceId = context.Scheduler.SchedulerInstanceId,
                RecordDate = DateTimeOffset.Now.DateTime
            };

            await ExecuteDal<MetricsData>(d => d.AddCocurentQueueItem(item));
        }

        private async Task FillAnomaly(DbJobInstanceLog item)
        {
            var statistics = await SafeGetJobStatistics();
            StatisticsUtil.SetAnomaly(item, statistics);

            if (item.Anomaly != null)
            {
                var parameters = new { item.InstanceId, item.Anomaly };
                await ExecuteDal<HistoryData>(d => d.SetAnomaly(parameters));
            }
        }

        private async Task<JobStatistics> SafeGetJobStatistics()
        {
            try
            {
                var result = await GetJobStatistics();
                return result;
            }
            catch (ObjectDisposedException)
            {
                var result = await GetJobStatisticsRaw();
                return result;
            }
        }

        private async Task<JobStatistics> GetJobStatisticsRaw()
        {
            var durationStatistics = await ExecuteDal<MetricsData, IEnumerable<JobDurationStatistic>>(d => d.GetJobDurationStatistics());
            var effectedStatistics = await ExecuteDal<MetricsData, IEnumerable<JobEffectedRowsStatistic>>(d => d.GetJobEffectedRowsStatistics());
            return new JobStatistics
            {
                JobDurationStatistics = durationStatistics,
                JobEffectedRowsStatistic = effectedStatistics
            };
        }

        private async Task<JobStatistics> GetJobStatistics()
        {
            var durationKey = nameof(MetricsData.GetJobDurationStatistics);
            var effectedKey = nameof(MetricsData.GetJobEffectedRowsStatistics);

            using var scope = ServiceScopeFactory.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

            var exists = cache.TryGetValue<IEnumerable<JobDurationStatistic>>(durationKey, out var durationStatistics);
            if (!exists || durationStatistics == null)
            {
                durationStatistics = await ExecuteDal<MetricsData, IEnumerable<JobDurationStatistic>>(d => d.GetJobDurationStatistics());
                cache.Set(durationKey, durationStatistics, StatisticsUtil.DefaultCacheSpan);
            }

            exists = cache.TryGetValue<IEnumerable<JobEffectedRowsStatistic>>(effectedKey, out var effectedStatistics);
            if (!exists || effectedStatistics == null)
            {
                effectedStatistics = await ExecuteDal<MetricsData, IEnumerable<JobEffectedRowsStatistic>>(d => d.GetJobEffectedRowsStatistics());
                cache.Set(effectedKey, effectedStatistics, StatisticsUtil.DefaultCacheSpan);
            }

            return new JobStatistics
            {
                JobDurationStatistics = durationStatistics,
                JobEffectedRowsStatistic = effectedStatistics
            };
        }

        private async Task SafeFillAnomaly(DbJobInstanceLog item)
        {
            try
            {
                await FillAnomaly(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"fail to invoke {nameof(FillAnomaly)} in {nameof(LogJobListener)}");
            }
        }

        private void SafeMonitorJobWasExecuted(IJobExecutionContext context, Exception? exception)
        {
            SafeScan(MonitorEvents.ExecutionEnd, context, exception);
            SafeScan(MonitorEvents.ExecutionEndWithEffectedRowsGreaterThanx, context, exception);
            SafeScan(MonitorEvents.ExecutionEndWithEffectedRowsLessThanx, context, exception);
            SafeScan(MonitorEvents.ExecutionEndWithEffectedRowsGreaterThanxInyHours, context, exception);
            SafeScan(MonitorEvents.ExecutionEndWithEffectedRowsLessThanxInyHours, context, exception);

            var success = exception == null;
            if (success)
            {
                SafeScan(MonitorEvents.ExecutionSuccess, context, exception);

                // Execution sucsses with no effected rows
                var effectedRows = ServiceUtil.GetEffectedRows(context);
                if (effectedRows == 0)
                {
                    SafeScan(MonitorEvents.ExecutionSuccessWithNoEffectedRows, context, exception);
                }
            }
            else
            {
                SafeScan(MonitorEvents.ExecutionFail, context, exception);
                SafeScan(MonitorEvents.ExecutionFailxTimesInRow, context, exception);
                SafeScan(MonitorEvents.ExecutionFailxTimesInyHours, context, exception);
            }
        }
    }
}