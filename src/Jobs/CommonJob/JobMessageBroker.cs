using CommonJob.MessageBrokerEntities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Job;
using System;
using System.Collections.Generic;
using IJobExecutionContext = Quartz.IJobExecutionContext;

namespace CommonJob
{
    public class JobMessageBroker
    {
        private static readonly object Locker = new();
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

        public void AddAggregateException(ExceptionDto ex)
        {
            if (ex == null) { return; }
            lock (Locker)
            {
                Metadata.Exceptions.Add(ex);
            }
        }

        public void AppendLog(LogLevel level, string messag)
        {
            var formatedMessage = $"[{DateTime.Now:HH:mm:ss} {GetLogLevelDisplayTest(level)}] {messag}";
            var log = new LogEntity(level, formatedMessage);
            LogData(log);
        }

        public void AppendLog(LogEntity log)
        {
            if (log == null) { return; }
            LogData(log);
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
                throw new PlanarJobException($"the data key {item.Key} in invalid");
            }

            lock (Locker)
            {
                var value = PlanarConvert.ToString(item.Value);
                _context.JobDetail.JobDataMap.Put(item.Key, value);
            }
        }

        public void PutTriggerData(KeyValueObject item)
        {
            if (item == null) { return; }
            if (!Consts.IsDataKeyValid(item.Key))
            {
                throw new PlanarJobException($"the data key {item.Key} in invalid");
            }

            lock (Locker)
            {
                var value = PlanarConvert.ToString(item.Value);
                _context.Trigger.JobDataMap.Put(item.Key, value);
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

        public void UpdateProgress(byte value)
        {
            lock (Locker)
            {
                if (value > 100) { value = 100; }
                if (value < 0) { value = 0; }
                if (Metadata.Progress == value) { return; }
                Metadata.Progress = value;
            }

            _monitorUtil?.Scan(MonitorEvents.ExecutionProgressChanged, _context);
        }

        private static string GetLogLevelDisplayTest(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "TRC",
                LogLevel.Debug => "DBG",
                LogLevel.Information => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Critical => "CRT",
                _ => "NON", // case LogLevel.None
            };
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
            lock (Locker)
            {
                LogLevel = level;
                Metadata.Log.AppendLine($"[Log Level: {LogLevel}]");
            }
        }
    }
}