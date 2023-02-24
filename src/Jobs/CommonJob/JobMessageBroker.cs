using CommonJob.MessageBrokerEntities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar;
using Planar.Common;
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

        public JobMessageBroker(IJobExecutionContext context, Dictionary<string, string> settings)
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

        public void AppendLog(LogLevel level, string messag)
        {
            var formatedMessage = $"[{DateTime.Now:HH:mm:ss} {level}] | {messag}";
            var log = new LogEntity(level, formatedMessage);
            LogData(log);
        }

        public string Publish(string channel, string message)
        {
            switch (channel)
            {
                case "PutJobData":
                    var data1 = Deserialize<KeyValueItem>(message);
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
                    var data3 = Deserialize<ExceptionDto>(message);
                    lock (Locker)
                    {
                        Metadata.Exceptions.Add(data3);
                    }
                    return null;

                case "AppendLog":
                    var data4 = Deserialize<LogEntity>(message);
                    LogData(data4);
                    return null;

                case "GetExceptionsText":
                    var exceptionText = Metadata.GetExceptionsText();
                    return exceptionText;

                case "CheckIfStopRequest":
                    return _context.CancellationToken.IsCancellationRequested.ToString();

                case "FailOnStopRequest":
                    _context.CancellationToken.ThrowIfCancellationRequested();
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
                        Metadata.Progress = progress;
                    }

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

        private static T Deserialize<T>(string message)
        {
            var result = JsonConvert.DeserializeObject<T>(message);
            return result;
        }

        private static JobExecutionContext MapContext(IJobExecutionContext context, Dictionary<string, string> settings)
        {
            var hasRetry = context.Trigger.JobDataMap.Contains(Consts.RetrySpan);
            bool? lastRetry = null;
            if (hasRetry)
            {
                lastRetry = context.Trigger.JobDataMap.GetIntValue(Consts.RetryCounter) > Consts.MaxRetries;
            }

            var result = new JobExecutionContext
            {
                JobSettings = settings,
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
                    Description = context.JobDetail.Description,
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
                    IsRetryTrigger = context.Trigger.Key.Name.StartsWith(Consts.RetryTriggerNamePrefix),
                },
                Environment = Global.Environment
            };

            return result;
        }

        private bool HasSettings(Dictionary<string, string> settings, string key)
        {
            if (settings == null) { return false; }
            if (settings.ContainsKey(key))
            {
                var value = settings[key];
                if (Enum.TryParse<LogLevel>(value, true, out var tempLevel))
                {
                    SetLogLevel(tempLevel);
                    return true;
                }
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

        private void SetLogLevel(Dictionary<string, string> settings)
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