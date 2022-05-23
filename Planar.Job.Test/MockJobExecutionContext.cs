using Quartz;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Planar.Job.Test
{
    public class MockJobExecutionContext : IJobExecutionContext
    {
        private readonly DateTime _now = DateTime.Now;
        private readonly IJobDetail _jobDetail = new MockJobDetails();
        private readonly ITrigger _triggerDetail = new MockTriggerDetails();

        public MockJobExecutionContext(Dictionary<string, object> dataMap, DateTime? overrideNow)
        {
            MergedJobDataMap = new JobDataMap();

            if (overrideNow.HasValue)
            {
                MergedJobDataMap.Add(Consts.NowOverrideValue, overrideNow);
                JobDetail.JobDataMap.Add(Consts.NowOverrideValue, overrideNow);
                Trigger.JobDataMap.Add(Consts.NowOverrideValue, overrideNow);
            }

            if (dataMap != null)
            {
                foreach (var item in dataMap)
                {
                    MergedJobDataMap.Add(item);
                }
            }

            FireTimeUtc = new DateTimeOffset(overrideNow ?? DateTime.Now);
            FireInstanceId = $"JobTest_{Environment.MachineName}_{Environment.UserName}_{GenerateFireInstanceId()}";
        }

        public bool Recovering => false;

        public int RefireCount => 0;

        public IJobDetail JobDetail => _jobDetail;

        public string FireInstanceId { get; set; }

        public DateTimeOffset FireTimeUtc { get; set; }

        public TimeSpan JobRunTime { get; set; }

        public DateTimeOffset? NextFireTimeUtc => _now;

        public DateTimeOffset? ScheduledFireTimeUtc => _now;

        public DateTimeOffset? PreviousFireTimeUtc => _now;

        public IJobDetail JobDetails => _jobDetail;

        public IScheduler Scheduler => null;

        public ITrigger Trigger => _triggerDetail;

        public ICalendar Calendar => null;

        public TriggerKey RecoveringTriggerKey => null;

        public IJob JobInstance => null;

        public object Result { get; set; }

        public CancellationToken CancellationToken => CancellationToken.None;

        public JobDataMap MergedJobDataMap { get; set; }

        private static string GenerateFireInstanceId()
        {
            var result = new StringBuilder();
            var offset = '0';
            for (var i = 0; i < 18; i++)
            {
                var @char = (char)RandomNumberGenerator.GetInt32(offset, offset + 10);
                result.Append(@char);
            }

            return result.ToString();
        }

        public object Get(object key)
        {
            throw new NotImplementedException();
        }

        public void Put(object key, object objectValue)
        {
            throw new NotImplementedException();
        }
    }
}