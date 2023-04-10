using CommonJob;
using Planar.API.Common.Entities;
using Quartz;
using System;
using System.Threading;

namespace Planar.Service.Monitor.Test
{
    // ****** ATTENTION: any changes should reflect in Monitor Util ******

    internal class TestJobExecutionContext : IJobExecutionContext
    {
        public TestJobExecutionContext(MonitorTestRequest request)
        {
            var metadata = new JobExecutionMetadata
            {
                EffectedRows = request.EffectedRows,
                Progress = 100,
            };

            Result = metadata;
            JobDetail = new TestJob();
            Trigger = new TestTrigger(JobDetail.Key);
            MergedJobDataMap = new JobDataMap();

            foreach (var item in JobDetail.JobDataMap)
            {
                MergedJobDataMap.Add(item.Key, item.Value);
            }

            foreach (var item in Trigger.JobDataMap)
            {
                MergedJobDataMap.Put(item.Key, item.Value);
            }
        }

        public IScheduler Scheduler => null;

        public ITrigger Trigger { get; private set; }

        public ICalendar Calendar => null;

        public bool Recovering => false;

        // ****** ATTENTION: any changes should reflect in Monitor Util ******

        public TriggerKey RecoveringTriggerKey => null;

        public int RefireCount => 0;

        public JobDataMap MergedJobDataMap { get; private set; }

        public IJobDetail JobDetail { get; private set; }

        public IJob JobInstance => null;

        public DateTimeOffset FireTimeUtc => DateTimeOffset.UtcNow.AddMinutes(-5);

        public DateTimeOffset? ScheduledFireTimeUtc => DateTimeOffset.UtcNow.AddMinutes(-6);

        public DateTimeOffset? PreviousFireTimeUtc => DateTimeOffset.UtcNow.AddMinutes(-10);

        public DateTimeOffset? NextFireTimeUtc => DateTimeOffset.UtcNow.AddMinutes(10);

        // ****** ATTENTION: any changes should reflect in Monitor Util ******

        public string FireInstanceId => "NON_CLUSTERED638093344653612239";

        public object? Result { get; set; }

        public TimeSpan JobRunTime => TimeSpan.FromMinutes(5);

        public CancellationToken CancellationToken => default;

        public object Get(object key)
        {
            throw new NotImplementedException();
        }

        public void Put(object key, object objectValue)
        {
            throw new NotImplementedException();
        }

        // ****** ATTENTION: any changes should reflect in Monitor Util ******
    }
}