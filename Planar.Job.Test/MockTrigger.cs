using Quartz;
using System;

namespace Planar.Job.Test
{
    public class MockTrigger : ITrigger
    {
        private readonly JobDataMap _jobDataMap;

        public MockTrigger()
        {
            _jobDataMap = new JobDataMap
            {
                { Consts.TriggerId, "UnitTest_TriggerId" }
            };
        }

        public TriggerKey Key => new("TestTrigger", "Default");

        public JobKey JobKey => throw new NotImplementedException();

        public string Description => throw new NotImplementedException();

        public string CalendarName => throw new NotImplementedException();

        public JobDataMap JobDataMap => _jobDataMap;

        public DateTimeOffset? FinalFireTimeUtc => throw new NotImplementedException();

        public int MisfireInstruction => throw new NotImplementedException();

        public DateTimeOffset? EndTimeUtc => throw new NotImplementedException();

        public DateTimeOffset StartTimeUtc => throw new NotImplementedException();

        public int Priority { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool HasMillisecondPrecision => throw new NotImplementedException();

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