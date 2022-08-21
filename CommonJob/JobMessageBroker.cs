using CommonJob.MessageBrokerEntities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar;
using Planar.Common;
using Quartz;
using System;
using System.Collections.Generic;

namespace CommonJob
{
    public class JobMessageBroker
    {
        private readonly IJobExecutionContext _context;
        private static readonly object Locker = new();

        public JobMessageBroker(IJobExecutionContext context, Dictionary<string, string> settings)
        {
            _context = context;
            var mapContext = MapContext(context, settings);
            SetLogLevel(settings);
            Details = JsonConvert.SerializeObject(mapContext);
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
            Metadata.Information.AppendLine($"[Log Level: {LogLevel}]");
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

        public string Details { get; set; }

        private LogLevel LogLevel { get; set; }

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

        public string Publish(string channel, string message)
        {
            switch (channel)
            {
                case "PutJobData":
                    var data1 = Deserialize<KeyValueItem>(message);
                    lock (Locker)
                    {
                        _context.JobDetail.JobDataMap.Put(data1.Key, data1.Value);
                    }
                    return null;

                case "PutTriggerData":
                    var data2 = Deserialize<KeyValueItem>(message);
                    lock (Locker)
                    {
                        _context.Trigger.JobDataMap.Put(data2.Key, data2.Value);
                    }
                    return null;

                case "AddAggragateException":
                    var data3 = Deserialize<ExceptionDto>(message);
                    lock (Locker)
                    {
                        Metadata.Exceptions.Add(data3);
                    }
                    return null;

                case "AppendInformation":
                    var data4 = Deserialize<LogEntity>(message);
                    lock (Locker)
                    {
                        if ((int)data4.Level >= (int)LogLevel)
                        {
                            Metadata.Information.AppendLine(data4.Message);
                        }
                    }
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
                    return Convert.ToString(data);

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

                default:
                    return null;
            }
        }

        private static T Deserialize<T>(string message)
        {
            var result = JsonConvert.DeserializeObject<T>(message);
            return result;
        }

        private JobExecutionMetadata Metadata
        {
            get
            {
                return JobExecutionMetadata.GetInstance(_context);
            }
        }
    }
}