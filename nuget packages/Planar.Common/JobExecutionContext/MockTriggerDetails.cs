﻿using Planar.Job;
using Planar.Job.Test.JobExecutionContext;
using System;

namespace Planar.Common
{
    internal class MockTriggerDetails : ITriggerDetail
    {
        private readonly DataMap _triggerDataMap;
        private readonly DateTimeOffset _now;
        private readonly MockKey _triggerKey;
        private readonly MockKey _jobKey;

        public MockTriggerDetails(IExecuteJobProperties properties)
        {
            _now = DateTimeOffset.Now;
            _triggerKey = new MockKey(properties);
            _jobKey = new MockKey(properties);
            _triggerDataMap = DataMapUtils.Convert(properties.TriggerData);
            Timeout = properties.TriggerTimeout;
            FinalFireTime = _now.AddDays(new Random().Next(1, 30));
        }

        public int MisfireInstruction => 0;
        public int Priority { get; set; } = 5;
        public bool HasMillisecondPrecision => true;
        public DateTimeOffset? EndTimeUtc => _now;
        public DateTimeOffset? FinalFireTimeUtc => _now;
        public DateTimeOffset StartTimeUtc => _now;
#if NETSTANDARD2_0
        public string CalendarName => null;
#else
        public string? CalendarName => null;
#endif
        public string Description => "This is UnitTest trigger description";
        public IKey JobKey => _jobKey;
        public IDataMap TriggerDataMap => _triggerDataMap;
        public IKey Key => _triggerKey;
        public DateTimeOffset? EndTime => null;
        public DateTimeOffset? FinalFireTime { get; private set; }
        public DateTimeOffset StartTime => _now;
        public bool HasRetry => false;
        public bool? IsLastRetry => HasRetry && IsRetryTrigger && RetryNumber == MaxRetries;
        public bool IsRetryTrigger => false;
        public TimeSpan? RetrySpan => null;
        public int? RetryNumber => null;
        public int? MaxRetries => null;
        public string Id { get; } = General.GenerateId();

        public TimeSpan? Timeout { get; private set; }
    }
}