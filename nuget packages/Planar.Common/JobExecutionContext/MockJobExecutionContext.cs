using Planar.Job;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;

namespace Planar.Common
{
    internal class MockJobExecutionContext : IJobExecutionContext
    {
        private readonly DateTimeOffset _start;
        private readonly DateTimeOffset _now;
        private readonly IJobDetail _jobDetail;
        private readonly ITriggerDetail _triggerDetail;

        public MockJobExecutionContext(IExecuteJobProperties properties)
        {
            _start = DateTimeOffset.UtcNow;
            _now = DateTimeOffset.Now;
            _jobDetail = new MockJobDetails(properties);
            _triggerDetail = new MockTriggerDetails(properties);

            NextFireTime = _now.AddHours(1);
            NextFireTimeUtc = _now.UtcDateTime.AddHours(1);

            if (properties.ExecutionDate.HasValue)
            {
                _now = properties.ExecutionDate.Value;
                ((DataMap)JobDetails.JobDataMap).Add(Consts.NowOverrideValue, _now.ToString());
                ((DataMap)Trigger.TriggerDataMap).Add(Consts.NowOverrideValue, _now.ToString());
            }

            MergedJobDataMap = ((DataMap)_jobDetail.JobDataMap).Merge((DataMap)_triggerDetail.TriggerDataMap);
            FireInstanceId = $"JobTest_{GenerateFireInstanceId()}";
            Recovering = properties.Recovering;
            RefireCount = properties.RefireCount;
            Environment = properties.Environment;

#if NETSTANDARD2_0
            if (properties.GlobalSettings.Any())
            {
                JobSettings = new Dictionary<string, string>(properties.GlobalSettings);
            }
            else
            {
                JobSettings = new Dictionary<string, string>();
            }
#else
            if (properties.GlobalSettings.Any())
            {
                JobSettings = new Dictionary<string, string?>(properties.GlobalSettings);
            }
            else
            {
                JobSettings = new Dictionary<string, string?>();
            }
#endif
        }

#if NETSTANDARD2_0
        public Dictionary<string, string> JobSettings { get; set; }
#else
        public Dictionary<string, string?> JobSettings { get; set; }
#endif

        public bool Recovering { get; private set; }

        public int RefireCount { get; private set; }

        public string FireInstanceId { get; private set; }

        public DateTimeOffset FireTimeUtc => _now.UtcDateTime;

        private TimeSpan? _jobRunTime;

        public TimeSpan JobRunTime
        {
            get
            {
                if (_jobRunTime == null)
                {
                    return DateTimeOffset.UtcNow.Subtract(_start);
                }

                return _jobRunTime.Value;
            }
            set
            {
                _jobRunTime = value;
            }
        }

        public DateTimeOffset? NextFireTimeUtc { get; private set; }

        public IJobDetail JobDetails => _jobDetail;

        public ITriggerDetail Trigger => _triggerDetail;

#if NETSTANDARD2_0
        public object Result { get; set; }
#else
        public object? Result { get; set; }
#endif

        public IDataMap MergedJobDataMap { get; internal set; }

        public DateTimeOffset FireTime => _now;

        public DateTimeOffset? NextFireTime { get; private set; }

        public DateTimeOffset? ScheduledFireTime => _now;

        public DateTimeOffset? PreviousFireTime => null;

        public ITriggerDetail TriggerDetails => _triggerDetail;

        public string Environment { get; private set; }

        [JsonIgnore]
        public CancellationToken CancellationToken { get; set; }

        private static string GenerateFireInstanceId()
        {
            var result = new StringBuilder();
            var offset = '0';
            for (var i = 0; i < 18; i++)
            {
#if NETSTANDARD2_0
                Random random = new Random(); // The using statement ensures proper disposal.
                var num = random.Next(offset, offset + 10);
                var @char = (char)num;
#else
                var @char = (char)RandomNumberGenerator.GetInt32(offset, offset + 10);
#endif
                result.Append(@char);
            }

            return result.ToString();
        }
    }
}