using Quartz;
using System;
using System.Collections.Generic;

namespace Planar.Service.Monitor.Test
{
    internal class TestJob : IJobDetail
    {
        public TestJob()
        {
            IDictionary<string, object> dict = new Dictionary<string, object>
            {
                { Consts.JobId, "fis4enyy1yp" },
                {Consts.Author, "Some Author" }
            };

            JobDataMap = new JobDataMap(dict);
        }

        public JobKey Key => new("Test", "TestJob");

        public string Description => "This is test job";

        public Type JobType => typeof(TestJob);

        public JobDataMap JobDataMap { get; set; }

        public bool Durable => true;

        public bool PersistJobDataAfterExecution => true;

        public bool ConcurrentExecutionDisallowed => false;

        public bool RequestsRecovery => false;

        public IJobDetail Clone()
        {
            return this;
        }

        public JobBuilder GetJobBuilder()
        {
            throw new NotImplementedException();
        }
    }
}