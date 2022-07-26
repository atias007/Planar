using Planar.Service.General;
using Quartz;
using System;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs
{
    public abstract class BaseSystemJob
    {
        protected static async Task<JobKey> Schedule<T>(IScheduler scheduler, string description, TimeSpan span)
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
                .WithDescription(description)
                .StoreDurably(true)
                .Build();
            }

            var triggerId = ServiceUtil.GenerateId();
            var startDate = new DateTimeOffset(DateTime.Now);
            startDate = startDate.AddSeconds(-startDate.Second);
            startDate = startDate.Add(span);

            var trigger = TriggerBuilder.Create()
                .WithIdentity(jobKey.Name, jobKey.Group)
                .StartAt(startDate)
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