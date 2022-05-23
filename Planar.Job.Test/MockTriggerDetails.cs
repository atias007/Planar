using Quartz;
using System;

namespace Planar.Job.Test
{
    public class MockTriggerDetails : ITrigger
    {
        private readonly JobDataMap _triggerDataMap;
        private readonly DateTime _now = DateTime.Now;
        private readonly TriggerKey _triggerKey = new("UnitTest", "Default");
        private readonly JobKey _jobKey = new("UnitTest", "Default");

        public MockTriggerDetails()
        {
            _triggerDataMap = new JobDataMap(1)
            {
                { Consts.TriggerId, "UnitTest_TriggerId" }
            };
        }

        public int MisfireInstruction => 0;
        public int Priority { get; set; } = 5;
        public bool HasMillisecondPrecision => true;
        public DateTimeOffset? EndTimeUtc => _now;
        public DateTimeOffset? FinalFireTimeUtc => _now;
        public DateTimeOffset StartTimeUtc => _now;
        public string CalendarName => null;
        public string Description => "This is UnitTest trigger description";
        public JobKey JobKey => _jobKey;

        public JobDataMap JobDataMap => _triggerDataMap;

        public TriggerKey Key => _triggerKey;

        public ITrigger Clone()
        {
            throw new NotImplementedException();
        }

        public int CompareTo(ITrigger other)
        {
            throw new NotImplementedException();
        }

        public DateTimeOffset? GetFireTimeAfter(DateTimeOffset? afterTime)
        {
            throw new NotImplementedException();
        }

        public bool GetMayFireAgain()
        {
            throw new NotImplementedException();
        }

        public DateTimeOffset? GetNextFireTimeUtc()
        {
            throw new NotImplementedException();
        }

        public DateTimeOffset? GetPreviousFireTimeUtc()
        {
            throw new NotImplementedException();
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
}