using Planar.Job;
using Planar.Job.Test.JobExecutionContext;
using System.IO;

namespace Planar.Common
{
    internal class MockJobDetails : IJobDetail
    {
        private readonly DataMap _jobDataMap;
        private readonly IKey _key;

        public MockJobDetails(IExecuteJobProperties properties)
        {
            _jobDataMap = DataMapUtils.Convert(properties.JobData);
            _key = new MockKey(properties.JobKeyName, properties.JobKeyGroup);
        }

        public IKey Key => _key;

        public string Description => "This is UnitTest job description";

        public IDataMap JobDataMap => _jobDataMap;

        public bool Durable => false;

        public bool PersistJobDataAfterExecution => false;

        public bool ConcurrentExecutionDisallowed => false;

        public bool RequestsRecovery => false;

        public string Id { get; } = General.GenerateId();
    }
}