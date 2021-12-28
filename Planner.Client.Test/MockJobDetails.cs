using Quartz;
using System;

namespace Planner.Common.Test
{
    public class MockJobDetails : IJobDetail
    {
        private readonly JobDataMap _jobDataMap;

        public MockJobDetails()
        {
            _jobDataMap = new JobDataMap
            {
                { Consts.JobId, "UnitTest_JobId" }
            };
        }

        public JobKey Key => new("TestJob", "Default");

        public string Description => throw new NotImplementedException();

        public Type JobType => throw new NotImplementedException();

        public JobDataMap JobDataMap => _jobDataMap;

        public bool Durable => throw new NotImplementedException();

        public bool PersistJobDataAfterExecution => throw new NotImplementedException();

        public bool ConcurrentExecutionDisallowed => throw new NotImplementedException();

        public bool RequestsRecovery => throw new NotImplementedException();

        public IJobDetail Clone()
        {
            throw new NotImplementedException();
        }

        public JobBuilder GetJobBuilder()
        {
            throw new NotImplementedException();
        }
    }
}