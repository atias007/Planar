using CommonJob.MessageBrokerEntities;
using Newtonsoft.Json;
using Planar;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;

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
            Details = JsonConvert.SerializeObject(mapContext);
        }

        public string Details { get; set; }

        private static JobExecutionContext MapContext(IJobExecutionContext context, Dictionary<string, string> settings)
        {
            var mergeData = context.MergedJobDataMap.ToDictionary(k => k.Key, v => Convert.ToString(v.Value));
            var result = new JobExecutionContext
            {
                JobSettings = settings,
                FireInstanceId = context.FireInstanceId,
                FireTime = context.FireTimeUtc,
                NextFireTime = context.NextFireTimeUtc,
                PreviousFireTime = context.PreviousFireTimeUtc,
                Recovering = context.Recovering,
                RefireCount = context.RefireCount,
                ScheduledFireTime = context.ScheduledFireTimeUtc
            };

            return result;
        }

        public string Publish(string channel, string message)
        {
            switch (channel)
            {
                case "PutJobData":
                    var data1 = Serialize<KeyValueItem>(message);
                    lock (Locker)
                    {
                        _context.JobDetail.JobDataMap.Put(data1.Key, data1.Value);
                    }
                    return null;

                case "PutTriggerData":
                    var data2 = Serialize<KeyValueItem>(message);
                    lock (Locker)
                    {
                        _context.JobDetail.JobDataMap.Put(data2.Key, data2.Value);
                    }
                    return null;

                case "AddAggragateException":
                    var data3 = Serialize<ExceptionDto>(message);
                    lock (Locker)
                    {
                        Metadata.Exceptions.Add(data3);
                    }
                    return null;

                case "AppendInformation":
                    lock (Locker)
                    {
                        Metadata.Information.AppendLine(message);
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

                default:
                    return null;
            }
        }

        private static T Serialize<T>(string message)
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