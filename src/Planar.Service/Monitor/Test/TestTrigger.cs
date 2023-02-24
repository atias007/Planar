using Quartz;
using System;
using System.Collections.Generic;

namespace Planar.Service.Monitor.Test
{
    // ****** ATTENTION: any changes should reflect in Monitor Util ******
    internal class TestTrigger : ITrigger
    {
        public TestTrigger(JobKey jobkey)
        {
            IDictionary<string, object> dict = new Dictionary<string, object>
            {
                { Consts.TriggerId, "h5qhcyhjh4l" }
            };

            JobKey = jobkey;
            JobDataMap = new JobDataMap(dict);
        }

        public TriggerKey Key => new("Test", "TestTrigger");

        public JobKey JobKey { get; set; }

        public string Description => "Test Trigger";

        public string CalendarName => "TestCalendar";

        public JobDataMap JobDataMap { get; set; }

        public DateTimeOffset? FinalFireTimeUtc => DateTimeOffset.UtcNow.AddMinutes(5);

        public int MisfireInstruction => 0;

        public DateTimeOffset? EndTimeUtc => DateTimeOffset.UtcNow;

        public DateTimeOffset StartTimeUtc => DateTimeOffset.UtcNow.AddMinutes(-5);

        public int Priority { get; set; } = 5;

        public bool HasMillisecondPrecision => true;

        public ITrigger Clone()
        {
            return this;
        }

        public int CompareTo(ITrigger other)
        {
            return 0;
        }

        public DateTimeOffset? GetFireTimeAfter(DateTimeOffset? afterTime)
        {
            return null;
        }

        public bool GetMayFireAgain()
        {
            return true;
        }

        public DateTimeOffset? GetNextFireTimeUtc()
        {
            return null;
        }

        public DateTimeOffset? GetPreviousFireTimeUtc()
        {
            return null;
        }

        public IScheduleBuilder GetScheduleBuilder()
        {
            throw new NotImplementedException();
        }

        public TriggerBuilder GetTriggerBuilder()
        {
            throw new NotImplementedException();
        }
    }

    // ****** ATTENTION: any changes should reflect in Monitor Util ******
}