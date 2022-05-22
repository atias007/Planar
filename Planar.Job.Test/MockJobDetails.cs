using System.Collections.Generic;

namespace Planar.Job.Test
{
    public class MockJobDetails : IJobDetail
    {
        private readonly SortedDictionary<string, string> _jobDataMap;
        private readonly IKey _key = new MockKey();

        public MockJobDetails()
        {
            _jobDataMap = new SortedDictionary<string, string>
            {
                { Consts.JobId, "UnitTest_JobId" }
            };
        }

        public IKey Key => _key;

        public string Description => "This is UnitTest job description";

        public SortedDictionary<string, string> JobDataMap => _jobDataMap;

        public bool Durable => false;

        public bool PersistJobDataAfterExecution => false;

        public bool ConcurrentExecutionDisallowed => false;

        public bool RequestsRecovery => false;
    }
}