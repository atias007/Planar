﻿using CommonJob;
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

namespace Planar.Service.Listeners;

public class LogJobListener(IServiceScopeFactory serviceScopeFactory, ILogger<LogJobListener> logger)
    : BaseListener<LogJobListener>(serviceScopeFactory, logger), IJobListener
{
    public string Name => nameof(LogJobListener);

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private static string GetTriggerId(IJobExecutionContext context)
    {
        var isSequence = JobHelper.IsSequenceJob(context.MergedJobDataMap);
        if (isSequence) { return Consts.SequenceTriggerId; }

        var result = TriggerHelper.GetTriggerId(context.Trigger);
        if (!string.IsNullOrWhiteSpace(result)) { return result; }

        return Consts.ManualTriggerId;
    }

    private static bool IsRetry(IJobExecutionContext context)
    {
        return context.Trigger.Key.Group == Consts.RetryTriggerGroup;
    }

    private static DateTime StartDate(IJobExecutionContext context)
    {
        return context.FireTimeUtc.ToLocalTime().DateTime;
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
                StartDate = StartDate(context),
                Status = (int)StatusMembers.Running,
                StatusTitle = StatusMembers.Running.ToString(),
                JobId = JobKeyHelper.GetJobId(context.JobDetail) ?? string.Empty,
                JobName = context.JobDetail.Key.Name,
                JobGroup = context.JobDetail.Key.Group,
                JobType = SchedulerUtil.GetJobTypeName(context.JobDetail.JobType),
                TriggerId = GetTriggerId(context),
                TriggerName = context.Trigger.Key.Name,
                TriggerGroup = context.Trigger.Key.Group,
                Retry = IsRetry(context),
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

            await ExecuteDal<IHistoryData>(d => d.CreateJobInstanceLog(log));
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
            var hasWarnings = ServiceUtil.HasWarnings(context);
            var log = new DbJobInstanceLog
            {
                JobId = JobKeyHelper.GetJobId(context.JobDetail) ?? string.Empty, // for function: SafeFillAnomaly(log)
                InstanceId = context.FireInstanceId,
                Duration = Convert.ToInt32(duration),
                EndDate = endDate,
                Exception = GetExceptionText(executionException),
                ExceptionCount = metadata?.Exceptions.Count() ?? 0,
                EffectedRows = metadata?.EffectedRows,
                Log = metadata?.GetLogText(),
                Status = (int)status,
                StatusTitle = status.ToString(),
                IsCanceled = context.CancellationToken.IsCancellationRequested,
                HasWarnings = hasWarnings
            };

            log.Log?.Trim();
            if (log.StatusTitle.Length > 10) { log.StatusTitle = log.StatusTitle[0..10]; }

            await ExecuteDal<IHistoryData>(d => d.UpdateHistoryJobRunLog(log));

            var lastLog = new HistoryLastLog
            {
                Duration = log.Duration,
                EffectedRows = log.EffectedRows,
                HasWarnings = hasWarnings,
                JobId = log.JobId,
                InstanceId = log.InstanceId,
                Status = log.Status,
                IsCanceled = log.IsCanceled,
                StatusTitle = log.StatusTitle,
                JobGroup = context.JobDetail.Key.Group,
                JobName = context.JobDetail.Key.Name,
                Retry = IsRetry(context),
                ServerName = Environment.MachineName,
                StartDate = StartDate(context),
                TriggerId = GetTriggerId(context),
                JobType = SchedulerUtil.GetJobTypeName(context.JobDetail.JobType),
            };
            await ExecuteDal<IHistoryData>(d => d.MergeHistoryLastLog(lastLog));

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

        var yml = YmlUtil.Serialize(items!);
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

        await ExecuteDal<IMetricsData>(d => d.AddCocurentQueueItem(item));
    }

    private async Task FillAnomaly(DbJobInstanceLog item)
    {
        var statistics = await SafeGetJobStatistics();
        if (statistics == null) { return; }
        StatisticsUtil.SetAnomaly(item, statistics);

        if (item.Anomaly != null)
        {
            await ExecuteDal<IHistoryData>(d => d.SetAnomaly(item));
        }
    }

    private async Task<JobStatistics?> SafeGetJobStatistics()
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

    private async Task<JobStatistics?> GetJobStatisticsRaw()
    {
        var durationStatistics = await ExecuteDal<IMetricsData, IEnumerable<JobDurationStatistic>>(d => d.GetJobDurationStatistics());
        var effectedStatistics = await ExecuteDal<IMetricsData, IEnumerable<JobEffectedRowsStatistic>>(d => d.GetJobEffectedRowsStatistics());

        if (durationStatistics == null) { return null; }
        if (effectedStatistics == null) { return null; }

        return new JobStatistics
        {
            JobDurationStatistics = durationStatistics,
            JobEffectedRowsStatistic = effectedStatistics
        };
    }

    private async Task<JobStatistics?> GetJobStatistics()
    {
        var durationKey = nameof(IMetricsData.GetJobDurationStatistics);
        var effectedKey = nameof(IMetricsData.GetJobEffectedRowsStatistics);

        using var scope = ServiceScopeFactory.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        var exists = cache.TryGetValue<IEnumerable<JobDurationStatistic>>(durationKey, out var durationStatistics);
        if (!exists || durationStatistics == null)
        {
            durationStatistics = await ExecuteDal<IMetricsData, IEnumerable<JobDurationStatistic>>(d => d.GetJobDurationStatistics());
            if (durationStatistics != null)
            {
                cache.Set(durationKey, durationStatistics, StatisticsUtil.DefaultCacheSpan);
            }
        }

        exists = cache.TryGetValue<IEnumerable<JobEffectedRowsStatistic>>(effectedKey, out var effectedStatistics);
        if (!exists || effectedStatistics == null)
        {
            effectedStatistics = await ExecuteDal<IMetricsData, IEnumerable<JobEffectedRowsStatistic>>(d => d.GetJobEffectedRowsStatistics());
            if (effectedStatistics != null)
            {
                cache.Set(effectedKey, effectedStatistics, StatisticsUtil.DefaultCacheSpan);
            }
        }

        if (durationStatistics == null) { return null; }
        if (effectedStatistics == null) { return null; }

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
            _logger.LogError(ex, "fail to invoke {Method} in {Class}", nameof(FillAnomaly), nameof(LogJobListener));
        }
    }

    private void SafeMonitorJobWasExecuted(IJobExecutionContext context, Exception? exception)
    {
        SafeScan(MonitorEvents.ExecutionEnd, context, exception);
        SafeScan(MonitorEvents.ExecutionEndWithEffectedRowsGreaterThanx, context, exception);
        SafeScan(MonitorEvents.ExecutionEndWithEffectedRowsLessThanx, context, exception);
        SafeScan(MonitorEvents.ExecutionEndWithEffectedRowsGreaterThanxInyHours, context, exception);
        SafeScan(MonitorEvents.ExecutionEndWithEffectedRowsLessThanxInyHours, context, exception);
        SafeScan(MonitorEvents.ExecutionEndWithMoreThanxExceptions, context, exception);

        var success = exception == null;
        if (success)
        {
            // Execution Success
            SafeScan(MonitorEvents.ExecutionSuccess, context, exception);

            // Execution Sucsses With No Effected Rows
            var effectedRows = ServiceUtil.GetEffectedRows(context);
            if (effectedRows == 0)
            {
                SafeScan(MonitorEvents.ExecutionSuccessWithNoEffectedRows, context, exception);
            }

            var hasWarnings = ServiceUtil.HasWarnings(context);
            if (hasWarnings)
            {
                SafeScan(MonitorEvents.ExecutionSuccessWithWarnings, context, exception);
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