using CommonJob.MessageBrokerEntities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Job;
using System;
using System.Collections.Generic;
using System.Threading;
using IJobExecutionContext = Quartz.IJobExecutionContext;

namespace CommonJob
{
    public class JobMessageBroker
    {
        private static readonly object Locker = new();
        private readonly CancellationToken _cancellationToken;
        private readonly IJobExecutionContext _context;
        private readonly IMonitorUtil? _monitorUtil;

        public JobMessageBroker(IJobExecutionContext context, IDictionary<string, string?> settings, IMonitorUtil? monitorUtil)
            : this(context, settings)
        {
            _monitorUtil = monitorUtil;
        }

        public JobMessageBroker(IJobExecutionContext context, IDictionary<string, string?> settings)
        {
            _context = context;
            var mapContext = MapContext(context, settings);
            SetLogLevel(settings);
            Details = JsonConvert.SerializeObject(mapContext);
            _cancellationToken = context.CancellationToken;
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

        public void AppendLog(LogLevel level, string messag)
        {
            var formatedMessage = $"[{DateTime.Now:HH:mm:ss} {level}] | {messag}";
            var log = new LogEntity(level, formatedMessage);
            LogData(log);
        }

        public void IncreaseEffectedRows(int delta = 1)
        {
            lock (Locker)
            {
                Metadata.EffectedRows = Metadata.EffectedRows.GetValueOrDefault() + delta;
            }
        }

        public string? Publish(string channel, string message)
        {
            switch (channel)
            {
                case "PutJobData":
                    var data1 = Deserialize<KeyValueItem>(message);
                    if (data1 == null) { return null; }
                    if (!Consts.IsDataKeyValid(data1.Key))
                    {
                        throw new PlanarJobException($"the data key {data1.Key} in invalid");
                    }

                    lock (Locker)
                    {
                        var value = PlanarConvert.ToString(data1.Value);
                        _context.JobDetail.JobDataMap.Put(data1.Key, value);
                    }
                    return null;

                case "PutTriggerData":
                    var data2 = Deserialize<KeyValueItem>(message);
                    if (data2 == null) { return null; }
                    if (!Consts.IsDataKeyValid(data2.Key))
                    {
                        throw new PlanarJobException($"the data key {data2.Key} in invalid");
                    }

                    lock (Locker)
                    {
                        var value = PlanarConvert.ToString(data2.Value);
                        _context.Trigger.JobDataMap.Put(data2.Key, value);
                    }
                    return null;

                case "AddAggragateException":
                case "AddAggregateException":
                    var data3 = Deserialize<ExceptionDto>(message);
                    if (data3 == null) { return null; }
                    lock (Locker)
                    {
                        Metadata.Exceptions.Add(data3);
                    }
                    return null;

                case "AppendLog":
                    var data4 = Deserialize<LogEntity>(message);
                    if (data4 == null) { return null; }
                    LogData(data4);
                    return null;

                case "GetExceptionsText":
                    var exceptionText = Metadata.GetExceptionsText();
                    return exceptionText;

                case "CheckIfStopRequest":
                    return _context.CancellationToken.IsCancellationRequested.ToString();

                case "FailOnStopRequest":
                    if (_context.CancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException("job was cancelled");
                    }
                    return null;

                case "GetData":
                    var data = _context.MergedJobDataMap[message];
                    return PlanarConvert.ToString(data);

                case "IsDataExists":
                    return _context.MergedJobDataMap.ContainsKey(message).ToString();

                case "GetEffectedRows":
                    return Metadata.EffectedRows.ToString();

                case "IncreaseEffectedRows":
                    lock (Locker)
                    {
                        _ = int.TryParse(message, out var delta);
                        Metadata.EffectedRows = Metadata.EffectedRows.GetValueOrDefault() + delta;
                    }

                    return null;

                case "SetEffectedRows":
                    lock (Locker)
                    {
                        _ = int.TryParse(message, out var value);
                        Metadata.EffectedRows = value;
                    }

                    return null;

                case "UpdateProgress":
                    lock (Locker)
                    {
                        _ = byte.TryParse(message, out var progress);
                        if (Metadata.Progress == progress) { return null; }
                        Metadata.Progress = progress;
                    }

                    _monitorUtil?.Scan(MonitorEvents.ExecutionProgressChanged, _context);

                    return null;

                case "JobRunTime":
                    return _context.JobRunTime.TotalMilliseconds.ToString();

                case "DataContainsKey":
                    var contains = _context.MergedJobDataMap.ContainsKey(message);
                    return contains.ToString();

                default:
                    return null;
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

        public void UpdateProgress(byte progress)
        {
            lock (Locker)
            {
                Metadata.Progress = progress;
            }
        }

        public CancellationToken CreateLinkedToken()
        {
            var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
            return linkedCancellationTokenSource.Token;
        }

        private static T? Deserialize<T>(string message)
        {
            var result = JsonConvert.DeserializeObject<T>(message);
            return result;
        }

        private static JobExecutionContext MapContext(IJobExecutionContext context, IDictionary<string, string?> settings)
        {
            var isRetryTrigger = TriggerHelper.IsRetryTrigger(context.Trigger);
            var hasRetry = TriggerHelper.HasRetry(context.Trigger);
            var retryNumber = TriggerHelper.GetRetryNumber(context.Trigger);
            var maxRetries = TriggerHelper.GetMaxRetries(context.Trigger);
            var retrySpan = TriggerHelper.GetRetrySpan(context.Trigger);
            var lastRetry = hasRetry ? retryNumber >= maxRetries : (bool?)null;

            var result = new JobExecutionContext
            {
                JobSettings = new Dictionary<string, string?>(settings),
                MergedJobDataMap = Global.ConvertDataMapToDictionary(context.MergedJobDataMap),
                FireInstanceId = context.FireInstanceId,
                FireTime = context.FireTimeUtc,
                NextFireTime = context.NextFireTimeUtc,
                PreviousFireTime = context.PreviousFireTimeUtc,
                Recovering = context.Recovering,
                RefireCount = context.RefireCount,
                ScheduledFireTime = context.ScheduledFireTimeUtc,
                JobDetails = new JobDetail
                {
                    ConcurrentExecutionDisallowed = context.JobDetail.ConcurrentExecutionDisallowed,
                    Description = context.JobDetail.Description ?? string.Empty,
                    Durable = context.JobDetail.Durable,
                    JobDataMap = Global.ConvertDataMapToDictionary(context.JobDetail.JobDataMap),
                    Key = new Key
                    {
                        Name = context.JobDetail.Key.Name,
                        Group = context.JobDetail.Key.Group
                    },
                    PersistJobDataAfterExecution = context.JobDetail.PersistJobDataAfterExecution,
                    RequestsRecovery = context.JobDetail.RequestsRecovery
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
                    TriggerDataMap = Global.ConvertDataMapToDictionary(context.Trigger.JobDataMap),
                    HasRetry = hasRetry,
                    IsLastRetry = lastRetry,
                    IsRetryTrigger = isRetryTrigger,
                    MaxRetries = maxRetries,
                    RetryNumber = retryNumber == 0 ? null : retryNumber,
                    RetrySpan = retrySpan
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
                    Metadata.Log.AppendLine(logEntity.Message);
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
            LogLevel = level;
            Metadata.Log.AppendLine($"[Log Level: {LogLevel}]");
        }
    }
}