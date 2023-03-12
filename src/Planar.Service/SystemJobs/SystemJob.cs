using Planar.Service.General;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs
{
    public abstract class SystemJob
    {
        protected static async Task<JobKey> Schedule<T>(IScheduler scheduler, string description, TimeSpan span, DateTime? startDate = null, CancellationToken stoppingToken = default)
            where T : IJob
        {
            var name = typeof(T).Name;
            var jobKey = new JobKey(name, Consts.PlanarSystemGroup);
            var job = await scheduler.GetJobDetail(jobKey, stoppingToken);

            if (job != null) { return jobKey; }

            var jobId = ServiceUtil.GenerateId();
            job = JobBuilder.Create(typeof(T))
            .WithIdentity(jobKey)
            .UsingJobData(Consts.JobId, jobId)
            .DisallowConcurrentExecution()
            .PersistJobDataAfterExecution()
            .WithDescription(description)
            .StoreDurably(true)
            .Build();

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
                .WithSimpleSchedule(s => BuildScheduler(s, span))
                .WithPriority(int.MinValue)
                .Build();

            await scheduler.ScheduleJob(job, new[] { trigger }, true, stoppingToken);

            return jobKey;
        }

        private static void BuildScheduler(SimpleScheduleBuilder builder, TimeSpan span)
        {
            builder
                .WithInterval(span)
                .RepeatForever();

            if (span.TotalMinutes > 15)
            {
                builder.WithMisfireHandlingInstructionFireNow();
            }
            else
            {
                builder.WithMisfireHandlingInstructionNextWithExistingCount();
            }
        }
    }
}