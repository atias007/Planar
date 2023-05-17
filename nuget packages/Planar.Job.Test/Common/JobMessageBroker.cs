using CommonJob.MessageBrokerEntities;
using Microsoft.Extensions.Logging;
using Planar.Job.Test.Common;
using System;
using System.Collections.Generic;
using System.Text.Json;
using YamlDotNet.Core.Tokens;

namespace Planar.Job.Test
{
    internal class JobMessageBroker
    {
        private readonly DateTimeOffset _startTime = DateTime.UtcNow;
        private static readonly object Locker = new object();
        private readonly MockJobExecutionContext _context;

        public JobMessageBroker(MockJobExecutionContext context, Dictionary<string, string?> settings)
        {
            _context = context;
            context.JobSettings = settings;
            SetLogLevel(settings);
            Details = JsonSerializer.Serialize(context);
        }

        public string Details { get; set; }

        private LogLevel LogLevel { get; set; }

        private JobExecutionMetadata Metadata
        {
            get
            {
                var result = JobExecutionMetadata.GetInstance(_context);
                return result ?? throw new NullReferenceException(nameof(Metadata));
            }
        }

        public string? Publish(string channel, string message)
        {
            switch (channel)
            {
                case "PutJobData":
                    var data1 = Deserialize<KeyValueItem>(message);
                    if (!Consts.IsDataKeyValid(data1.Key))
                    {
                        throw new PlanarJobTestException($"the data key {data1.Key} in invalid");
                    }

                    lock (Locker)
                    {
                        var value = PlanarConvert.ToString(data1.Value);
                        _context.JobDetails.JobDataMap.AddOrUpdate(data1.Key, value);
                        _context.MergedJobDataMap = _context.JobDetails.JobDataMap.Merge(_context.TriggerDetails.TriggerDataMap);
                    }
                    return null;

                case "PutTriggerData":
                    var data2 = Deserialize<KeyValueItem>(message);
                    if (!Consts.IsDataKeyValid(data2.Key))
                    {
                        throw new PlanarJobTestException($"the data key {data2.Key} in invalid");
                    }

                    lock (Locker)
                    {
                        var value = PlanarConvert.ToString(data2.Value);
                        _context.TriggerDetails.TriggerDataMap.AddOrUpdate(data2.Key, value);
                        _context.MergedJobDataMap = _context.JobDetails.JobDataMap.Merge(_context.TriggerDetails.TriggerDataMap);
                    }
                    return null;

                case "AddAggragateException":
                case "AddAggregateException":
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
                    return false.ToString();

                case "FailOnStopRequest":
                    return null;

                case "GetData":
                    try
                    {
                        var data = _context.MergedJobDataMap[message];
                        return PlanarConvert.ToString(data);
                    }
                    catch (KeyNotFoundException)
                    {
                        throw new PlanarJobTestException($"Key '{message}' was not found in merged data map");
                    }

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
                    return DateTimeOffset.UtcNow.Subtract(_startTime).TotalMilliseconds.ToString();

                case "DataContainsKey":
                    var contains = _context.MergedJobDataMap.ContainsKey(message);
                    return contains.ToString();

                default:
                    return null;
            }
        }

        private static T Deserialize<T>(string message)
            where T : class, new()
        {
            var result = JsonSerializer.Deserialize<T>(message);
            result ??= Activator.CreateInstance<T>();
            return result;
        }

        private bool HasSettings(Dictionary<string, string?> settings, string key)
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

        private void SetLogLevel(Dictionary<string, string?> settings)
        {
            if (HasSettings(settings, Consts.LogLevelSettingsKey1)) { return; }
            if (HasSettings(settings, Consts.LogLevelSettingsKey2)) { return; }
            SetLogLevel(LogLevel.Debug);
        }

        private void SetLogLevel(LogLevel level)
        {
            LogLevel = level;
            Metadata.Log.AppendLine($"[Log Level: {LogLevel}]");
        }
    }
}