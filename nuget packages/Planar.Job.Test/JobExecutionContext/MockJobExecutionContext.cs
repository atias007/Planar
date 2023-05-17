using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Planar.Job.Test
{
    internal class MockJobExecutionContext : IJobExecutionContext
    {
        private readonly DateTimeOffset _start;
        private readonly DateTimeOffset _now;
        private readonly IJobDetail _jobDetail;
        private readonly ITriggerDetail _triggerDetail;

        public MockJobExecutionContext(ExecuteJobProperties properties)
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
                JobDetail.JobDataMap.Add(Consts.NowOverrideValue, _now.ToString());
                Trigger.TriggerDataMap.Add(Consts.NowOverrideValue, _now.ToString());
            }

            MergedJobDataMap = _jobDetail.JobDataMap.Merge(_triggerDetail.TriggerDataMap);
            FireInstanceId = $"JobTest_{System.Environment.MachineName}_{System.Environment.UserName}_{GenerateFireInstanceId()}";
        }

        public Dictionary<string, string?> JobSettings { get; set; } = new Dictionary<string, string?>();

        public bool Recovering => false;

        public int RefireCount => 0;

        public IJobDetail JobDetail => _jobDetail;

        public string FireInstanceId { get; set; }

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

        public object? Result { get; set; }

        public SortedDictionary<string, string?> MergedJobDataMap { get; internal set; }

        public DateTimeOffset FireTime => _now;

        public DateTimeOffset? NextFireTime { get; private set; }

        public DateTimeOffset? ScheduledFireTime => _now.UtcDateTime;

        public DateTimeOffset? PreviousFireTime => null;

        public ITriggerDetail TriggerDetails => _triggerDetail;

        public string Environment => UnitTestConsts.Environment;

        private static string GenerateFireInstanceId()
        {
            var result = new StringBuilder();
            var offset = '0';
            for (var i = 0; i < 18; i++)
            {
                var @char = (char)RandomNumberGenerator.GetInt32(offset, offset + 10);
                result.Append(@char);
            }

            return result.ToString();
        }
    }
}