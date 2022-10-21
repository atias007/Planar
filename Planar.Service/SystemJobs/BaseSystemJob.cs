using Planar.Service.General;
using Quartz;
using System;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs
{
    public abstract class BaseSystemJob
    {
        protected static async Task<JobKey> Schedule<T>(IScheduler scheduler, string description, TimeSpan span, DateTime? startDate = null)
            where T : IJob
        {
            var name = typeof(T).Name;
            var jobKey = new JobKey(name, Consts.PlanarSystemGroup);
            var job = await scheduler.GetJobDetail(jobKey);

            if (job == null)
            {
                var jobId = ServiceUtil.GenerateId();
                job = JobBuilder.Create(typeof(T))
                .WithIdentity(jobKey)
                .UsingJobData(Consts.JobId, jobId)
                .DisallowConcurrentExecution()
                .PersistJobDataAfterExecution()
                .WithDescription(description)
                .StoreDurably(true)
                .Build();
            }

            var triggerId = ServiceUtil.GenerateId();
            DateTimeOffset jobStart;
            if (startDate == null)
            {
                jobStart = new DateTimeOffset(DateTime.Now);
                jobStart = jobStart.AddSeconds(-jobStart.Second);
                jobStart = jobStart.Add(span);
            }
            else
            {
                jobStart = new DateTimeOffset(startDate.Value);
            }

            var trigger = TriggerBuilder.Create()
                .WithIdentity(jobKey.Name, jobKey.Group)
                .StartAt(jobStart)
                .UsingJobData(Consts.TriggerId, triggerId)
                .WithSimpleSchedule(s => s
                    .WithInterval(span)
                    .RepeatForever()
                    .WithMisfireHandlingInstructionNextWithExistingCount()
                )
                .WithPriority(int.MaxValue)
                .Build();

            await scheduler.ScheduleJob(job, new[] { trigger }, true);

            return jobKey;
        }
    }
}