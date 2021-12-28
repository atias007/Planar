using Quartz;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Planner.Common.Test
{
    public class MockJobExecutionContext : IJobExecutionContext
    {
        public MockJobExecutionContext()
        {
            MergedJobDataMap = new JobDataMap();
        }

        public MockJobExecutionContext(IDictionary<string, object> dataMap)
        {
            if (dataMap == null) { dataMap = new Dictionary<string, object>(); }
            MergedJobDataMap = new JobDataMap(dataMap);
        }

        public IScheduler Scheduler => null;

        public ITrigger Trigger => new MockTrigger();

        public ICalendar Calendar => null;

        public bool Recovering => false;

        public TriggerKey RecoveringTriggerKey => new("UnitTestKeyName", "UnitTestKeyGroup");

        public int RefireCount => 0;

        public JobDataMap MergedJobDataMap { get; private set; }

        public IJobDetail JobDetail => new MockJobDetails();

        public IJob JobInstance => null;

        public DateTimeOffset FireTimeUtc { get; set; }

        public DateTimeOffset? ScheduledFireTimeUtc => new(DateTime.Now);

        public DateTimeOffset? PreviousFireTimeUtc => new(DateTime.Now.AddHours(-1));

        public DateTimeOffset? NextFireTimeUtc => new(DateTime.Now.AddHours(1));

        public string FireInstanceId { get; set; }

        public object Result { get; set; }

        public TimeSpan JobRunTime { get; set; }

        public CancellationToken CancellationToken => new(false);

        public object Get(object key)
        {
            return MergedJobDataMap.Get(Convert.ToString(key));
        }

        public void Put(object key, object objectValue)
        {
            if (MergedJobDataMap.ContainsKey(Convert.ToString(key)))
            {
                MergedJobDataMap[Convert.ToString(key)] = objectValue;
            }
            else
            {
                MergedJobDataMap.Add(Convert.ToString(key), objectValue);
            }
        }
    }
}