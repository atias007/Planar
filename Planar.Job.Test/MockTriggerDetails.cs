using System;
using System.Collections.Generic;

namespace Planar.Job.Test
{
    public class MockTriggerDetails : ITriggerDetail
    {
        private readonly SortedDictionary<string, string> _triggerDataMap;
        private readonly DateTime _now = DateTime.Now;
        private readonly IKey _key = new MockKey();

        public MockTriggerDetails()
        {
            _triggerDataMap = new SortedDictionary<string, string>
            {
                { Consts.TriggerId, "UnitTest_TriggerId" }
            };
        }

        public SortedDictionary<string, string> TriggerDataMap => _triggerDataMap;
        public int MisfireInstruction => 0;
        public int Priority => 5;
        public bool HasMillisecondPrecision => true;
        public DateTimeOffset? EndTime => _now;
        public DateTimeOffset? FinalFireTime => _now;
        public DateTimeOffset StartTime => _now;
        public string CalendarName => null;
        public string Description => "This is UnitTest trigger description";
        public IKey Key => _key;
    }
}