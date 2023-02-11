using Planar.Job.Test.Common;
using Planar.Job.Test.JobExecutionContext;
using System;
using System.Collections.Generic;

namespace Planar.Job.Test
{
    internal class MockTriggerDetails : ITriggerDetail
    {
        private readonly SortedDictionary<string, string> _triggerDataMap;
        private readonly DateTimeOffset _now;
        private readonly MockKey _triggerKey;
        private readonly MockKey _jobKey;

        public MockTriggerDetails(ExecuteJobProperties properties)
        {
            _now = DateTimeOffset.Now;
            _triggerKey = new MockKey(UnitTestConsts.Environment, UnitTestConsts.TriggerName);
            _jobKey = new MockKey(UnitTestConsts.Environment, UnitTestConsts.TestMethod);
            _triggerDataMap = new SortedDictionary<string, string>();

            if (properties.TriggerData != null)
            {
                foreach (var item in properties.TriggerData)
                {
                    _triggerDataMap.TryAdd(item.Key, Convert.ToString(item.Value));
                }
            }

            FinalFireTime = _now.AddDays(new Random().Next(1, 30));
        }

        public int MisfireInstruction => 0;
        public int Priority { get; set; } = 5;
        public bool HasMillisecondPrecision => true;
        public DateTimeOffset? EndTimeUtc => _now;
        public DateTimeOffset? FinalFireTimeUtc => _now;
        public DateTimeOffset StartTimeUtc => _now;
        public string CalendarName => null;
        public string Description => "This is UnitTest trigger description";
        public IKey JobKey => _jobKey;
        public SortedDictionary<string, string> TriggerDataMap => _triggerDataMap;
        public IKey Key => _triggerKey;
        public DateTimeOffset? EndTime => null;
        public DateTimeOffset? FinalFireTime { get; private set; }
        public DateTimeOffset StartTime => _now;
        public bool HasRetry => false;
        public bool? IsLastRetry => null;
        public bool IsRetryTrigger => false;
    }
}