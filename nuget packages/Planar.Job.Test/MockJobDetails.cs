using Quartz;
using System;
using System.Collections.Generic;

namespace Planar.Job.Test
{
    public class MockJobDetails : IJobDetail
    {
        private readonly JobDataMap _jobDataMap;
        private readonly JobKey _key = new JobKey("UnitTest", "Default");

        public MockJobDetails()
        {
            _jobDataMap = new JobDataMap(
             new SortedDictionary<string, string>
            {
               { Consts.JobId, "UnitTest_JobId" }
            });
        }

        public JobKey Key => _key;

        public string Description => "This is UnitTest job description";

        public JobDataMap JobDataMap => _jobDataMap;

        public bool Durable => false;

        public bool PersistJobDataAfterExecution => false;

        public bool ConcurrentExecutionDisallowed => false;

        public bool RequestsRecovery => false;

        public Type JobType => throw new NotImplementedException();

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