using Planar.Job.Test.JobExecutionContext;
using System.Collections.Generic;

namespace Planar.Job.Test
{
    internal class MockJobDetails : IJobDetail
    {
        private readonly SortedDictionary<string, string?> _jobDataMap;
        private readonly IKey _key = new MockKey(UnitTestConsts.Environment, UnitTestConsts.TestMethod);

        public MockJobDetails(ExecuteJobProperties properties)
        {
            _jobDataMap = DataMapUtils.Convert(properties.JobData);
        }

        public IKey Key => _key;

        public string Description => "This is UnitTest job description";

        public SortedDictionary<string, string?> JobDataMap => _jobDataMap;

        public bool Durable => false;

        public bool PersistJobDataAfterExecution => false;

        public bool ConcurrentExecutionDisallowed => false;

        public bool RequestsRecovery => false;
    }
}