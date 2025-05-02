using CommonJob.MessageBrokerEntities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Common.Helpers;
using Planar.Job;
using Planar.Service.API.Helpers;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IJobExecutionContext = Quartz.IJobExecutionContext;

namespace CommonJob;

// pub sub class for all jobs log messages
// each job instance has its own instance of this class
// this class is responsible for logging all messages and exceptions to job metadata
// this class is also responsible for raise event for each log message
// this class is also responsible for save history of all log messages
public sealed class JobLogBroker : IDisposable
{
    private readonly Lock Locker = new();
    private readonly IJobExecutionContext _context;
    private readonly IMonitorUtil _monitorUtil;

    public static event EventHandler<LogEntityEventArgs>? InterceptingLogMessage;

    public JobLogBroker(IJobExecutionContext context, IDictionary<string, string?> settings, IMonitorUtil monitorUtil)
    {
        _monitorUtil = monitorUtil;
        _context = context;
        var mapContext = MapContext(context, settings);
        SetLogLevel(settings);
        Details = JsonConvert.SerializeObject(mapContext);
    }

    public string Details { get; set; }

    private LogLevel LogLevel { get; set; }

    private JobExecutionMetadata Metadata
    {
        get
        {
            return JobExecutionMetadata.GetInstance(_context);
        }
    }

    private void OnInterceptingLogMessage(LogEntity log)
    {
        LogQueueFactory.Instance.Enqueue(_context.FireInstanceId, log);

        if (InterceptingLogMessage == null) { return; }
        var e = new LogEntityEventArgs(log, _context.FireInstanceId);
        InterceptingLogMessage(null, e);
    }

    private void OnInterceptingLogMessage(string message)
    {
        var log = new LogEntity(LogLevel.None, message);
        LogQueueFactory.Instance.Enqueue(_context.FireInstanceId, log);

        if (InterceptingLogMessage == null) { return; }
        var e = new LogEntityEventArgs(log, _context.FireInstanceId);
        InterceptingLogMessage(null, e);
    }

    public void AddAggregateException(ExceptionDto ex)
    {
        if (ex == null) { return; }
        lock (Locker)
        {
            Metadata.AddException(ex);
        }
    }

    public void AppendLog(LogLevel level, string messag)
    {
        var log = new LogEntity(level, messag);
        LogData(log);
    }

    public void AppendLog(LogEntity log)
    {
        if (log == null) { return; }
        LogData(log);
    }

    public void AppendLogRaw(string text)
    {
        lock (Locker)
        {
            OnInterceptingLogMessage(text);
            InnerAppendLogRaw(text);
        }
    }

    private void InnerAppendLogRaw(string text)
    {
        Metadata.AppendLog(text);
    }

    public void IncreaseEffectedRows(int delta = 1)
    {
        lock (Locker)
        {
            Metadata.EffectedRows = Metadata.EffectedRows.GetValueOrDefault() + delta;
        }
    }

    public void PutJobDataAction(KeyValueObject item)
    {
        if (item == null) { return; }
        if (!Consts.IsDataKeyValid(item.Key))
        {
            throw new PlanarException($"the data key {item.Key} in invalid");
        }

        lock (Locker)
        {
            var value = PlanarConvert.ToString(item.Value);
            _context.JobDetail.JobDataMap.Put(item.Key, value);
        }
    }

    public void RemoveJobDataAction(KeyValueObject item)
    {
        if (item == null) { return; }
        if (!Consts.IsDataKeyValid(item.Key))
        {
            throw new PlanarException($"the data key {item.Key} in invalid");
        }

        lock (Locker)
        {
            _context.JobDetail.JobDataMap.Remove(item.Key);
        }
    }

    public void ClearJobDataAction()
    {
        lock (Locker)
        {
            _context.JobDetail.JobDataMap.Clear();
        }
    }

    public void ClearTriggerDataAction()
    {
        lock (Locker)
        {
            _context.Trigger.JobDataMap.Clear();
        }
    }

    public void PutTriggerDataAction(KeyValueObject item)
    {
        if (item == null) { return; }
        if (!Consts.IsDataKeyValid(item.Key))
        {
            throw new PlanarException($"the data key {item.Key} in invalid");
        }

        lock (Locker)
        {
            var value = PlanarConvert.ToString(item.Value);
            _context.Trigger.JobDataMap.Put(item.Key, value);
        }
    }

    public void RemoveTriggerDataAction(KeyValueObject item)
    {
        if (item == null) { return; }
        if (!Consts.IsDataKeyValid(item.Key))
        {
            throw new PlanarException($"the data key {item.Key} in invalid");
        }

        lock (Locker)
        {
            _context.Trigger.JobDataMap.Remove(item.Key);
        }
    }

    public void SafeAppendLog(LogLevel level, string messag)
    {
        try
        {
            AppendLog(level, messag);
        }
        catch (Exception)
        {
            // === DO NOTHING ===
        }
    }

    public void SetEffectedRows(int value)
    {
        lock (Locker)
        {
            if (value < 0) { value = 0; }
            Metadata.EffectedRows = value;
        }
    }

    public void UpdateProgress(long current, long total)
    {
        var value = CalcProgress(current, total);
        UpdateProgress(value);
    }

    public void UpdateProgress(byte value)
    {
        lock (Locker)
        {
            if (value > 100) { value = 100; }
            if (value < 0) { value = 0; }
            if (Metadata.Progress == value) { return; }
            Metadata.Progress = value;
        }

        try
        {
            _monitorUtil?.Scan(MonitorEvents.ExecutionProgressChanged, _context);
        }
        catch (Exception ex)
        {
            throw new JobMonitorException("Fail to scan ExecutionProgressChanged monitor event", ex);
        }
    }

    private static byte CalcProgress(long current, long total)
    {
        var percentage = 1.0 * current / total * 100;
        if (percentage > byte.MaxValue) { percentage = byte.MaxValue; }
        if (percentage < byte.MinValue) { percentage = byte.MinValue; }
        var result = Convert.ToByte(percentage);
        return result;
    }

    private static DataMap ConvertDataMap(JobDataMap? map)
    {
        if (map == null)
        {
            return [];
        }

        var dic = map
            // .Where(k => Consts.IsDataKeyValid(k.Key)) // *** do the filter in job process (in BaseJob.FilterJobData)
            .OrderBy(k => k.Key)
            .ToDictionary(k => k.Key, v => PlanarConvert.ToString(v.Value));

        return new DataMap(dic);
    }

    private static JobExecutionContext MapContext(IJobExecutionContext context, IDictionary<string, string?> settings)
    {
        var isRetryTrigger = TriggerHelper.IsRetryTrigger(context.Trigger);
        var hasRetry = TriggerHelper.HasRetry(context.Trigger);
        var retryNumber = TriggerHelper.GetRetryNumber(context.Trigger);
        var maxRetries = TriggerHelper.GetMaxRetries(context.Trigger);
        var retrySpan = TriggerHelper.GetRetrySpan(context.Trigger);
        var lastRetry = hasRetry ? retryNumber >= maxRetries : (bool?)null;
        var jobId = JobHelper.GetJobId(context.JobDetail) ?? string.Empty;
        var triggerId = TriggerHelper.GetTriggerId(context.Trigger) ?? string.Empty;
        var timeout = TriggerHelper.GetTimeoutWithDefault(context.Trigger);

        var result = new JobExecutionContext
        {
            JobSettings = new Dictionary<string, string?>(settings),
            MergedJobDataMap = ConvertDataMap(context.MergedJobDataMap),
            FireInstanceId = context.FireInstanceId,
            FireTime = context.FireTimeUtc,
            NextFireTime = context.NextFireTimeUtc,
            PreviousFireTime = context.PreviousFireTimeUtc,
            Recovering = context.Recovering,
            JobPort = AppSettings.General.JobPort,
            RefireCount = context.RefireCount,
            ScheduledFireTime = context.ScheduledFireTimeUtc,
            JobDetails = new JobDetail
            {
                ConcurrentExecutionDisallowed = context.JobDetail.ConcurrentExecutionDisallowed,
                Description = context.JobDetail.Description ?? string.Empty,
                Durable = context.JobDetail.Durable,
                JobDataMap = ConvertDataMap(context.JobDetail.JobDataMap),
                Key = new Key
                {
                    Name = context.JobDetail.Key.Name,
                    Group = context.JobDetail.Key.Group
                },
                PersistJobDataAfterExecution = context.JobDetail.PersistJobDataAfterExecution,
                RequestsRecovery = context.JobDetail.RequestsRecovery,
                Id = jobId
            },
            TriggerDetails = new TriggerDetail
            {
                CalendarName = context.Trigger.CalendarName,
                Description = context.Trigger.Description,
                EndTime = context.Trigger.EndTimeUtc,
                FinalFireTime = context.Trigger.FinalFireTimeUtc,
                HasMillisecondPrecision = context.Trigger.HasMillisecondPrecision,
                Key = new Key
                {
                    Name = context.Trigger.Key.Name,
                    Group = context.Trigger.Key.Group
                },
                Priority = context.Trigger.Priority,
                StartTime = context.Trigger.StartTimeUtc,
                TriggerDataMap = ConvertDataMap(context.Trigger.JobDataMap),
                HasRetry = hasRetry,
                IsLastRetry = lastRetry,
                IsRetryTrigger = isRetryTrigger,
                MaxRetries = maxRetries,
                RetryNumber = retryNumber == 0 ? null : retryNumber,
                RetrySpan = retrySpan,
                Timeout = timeout,
                Id = triggerId
            },
            Environment = Global.Environment
        };

        return result;
    }

    private bool HasSettings(IDictionary<string, string?> settings, string key)
    {
        if (settings == null) { return false; }
        if (settings.TryGetValue(key, out string? value) && Enum.TryParse<LogLevel>(value, true, out var tempLevel))
        {
            SetLogLevel(tempLevel);
            return true;
        }

        return false;
    }

    private void LogData(LogEntity logEntity)
    {
        lock (Locker)
        {
            if ((int)logEntity.Level >= (int)LogLevel)
            {
                InnerAppendLogRaw(logEntity.ToString());
                OnInterceptingLogMessage(logEntity);
                if (logEntity.Level == LogLevel.Warning)
                {
                    Metadata.HasWarnings = true;
                }
            }
        }
    }

    private void SetLogLevel(IDictionary<string, string?> settings)
    {
        if (HasSettings(settings, Consts.LogLevelSettingsKey1)) { return; }
        if (HasSettings(settings, Consts.LogLevelSettingsKey2)) { return; }
        SetLogLevel(Global.LogLevel);
    }

    private void SetLogLevel(LogLevel level)
    {
        lock (Locker)
        {
            LogLevel = level;
            InnerAppendLogRaw($"[Log Level: {LogLevel}]");
        }
    }

    public void Dispose()
    {
        LogQueueFactory.Instance.Clear(_context.FireInstanceId);
    }
}