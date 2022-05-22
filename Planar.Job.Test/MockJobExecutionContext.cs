using System;
using System.Collections.Generic;

namespace Planar.Job.Test
{
    public class MockJobExecutionContext : IJobExecutionContext
    {
        private readonly DateTime _now = DateTime.Now;
        private readonly IJobDetail _jobDetail = new MockJobDetails();
        private readonly ITriggerDetail _triggerDetail = new MockTriggerDetails();
        private readonly SortedDictionary<string, string> _mergedJobDataMap;

        public MockJobExecutionContext()
        {
            _mergedJobDataMap = new();
        }

        public MockJobExecutionContext(SortedDictionary<string, string> dataMap)
        {
            if (dataMap == null) { dataMap = new SortedDictionary<string, string>(); }
            _mergedJobDataMap = dataMap;
        }

        public bool Recovering => false;

        public int RefireCount => 0;

        public IJobDetail JobDetail => _jobDetail;

        public string FireInstanceId { get; set; }

        public DateTimeOffset FireTime { get; set; }

        public TimeSpan JobRunTime { get; set; }

        public DateTimeOffset? NextFireTime => _now;

        public DateTimeOffset? ScheduledFireTime => _now;

        public DateTimeOffset? PreviousFireTime => _now;

        public IJobDetail JobDetails => new MockJobDetails();

        public ITriggerDetail TriggerDetails => _triggerDetail;

        public string Environment => "UnitTest";

        public SortedDictionary<string, string> MergedJobDataMap => _mergedJobDataMap;
    }
}