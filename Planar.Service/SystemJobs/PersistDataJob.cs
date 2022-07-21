using CommonJob;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.General;
using Polly;
using Quartz;
using System;
using System.Threading.Tasks;
using DbJobInstanceLog = Planar.Service.Model.JobInstanceLog;

namespace Planar.Service.SystemJobs
{
    [DisallowConcurrentExecution]
    public class PersistDataJob : IJob
    {
        private readonly ILogger<PersistDataJob> _logger;

        private readonly DataLayer _dal;

        public PersistDataJob()
        {
            _logger = Global.GetLogger<PersistDataJob>();
            _dal = Global.ServiceProvider.GetService<DataLayer>();
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                return DoWork();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to persist data: {Message}", ex.Message);
                return Task.CompletedTask;
            }
        }

        public static async Task Schedule(IScheduler scheduler)
        {
            var jobKey = new JobKey(nameof(PersistDataJob), Consts.PlanarSystemGroup);
            IJobDetail job = null;

            try
            {
                job = await scheduler.GetJobDetail(jobKey);
            }
            catch (Exception)
            {
                try
                {
                    await scheduler.DeleteJob(jobKey);
                }
                catch
                {
                    // *** DO NOTHING *** //
                }
                finally
                {
                    job = null;
                }
            }

            if (job != null)
            {
                await scheduler.DeleteJob(jobKey);
                job = await scheduler.GetJobDetail(jobKey);

                if (job != null) { return; }
            }

            var jobId = ServiceUtil.GenerateId();
            var triggerId = ServiceUtil.GenerateId();

            job = JobBuilder.Create(typeof(PersistDataJob))
                .WithIdentity(jobKey)
                .UsingJobData(Consts.JobId, jobId)
                .WithDescription("System job for persist information & exception from running jobs")
                .StoreDurably(true)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(jobKey.Name, jobKey.Group)
                .StartAt(new DateTimeOffset(DateTime.Now.Add(AppSettings.PersistRunningJobsSpan)))
                .UsingJobData(Consts.TriggerId, triggerId)
                .WithSimpleSchedule(s => s
                    .WithInterval(AppSettings.PersistRunningJobsSpan)
                    .RepeatForever()
                    .WithMisfireHandlingInstructionNextWithExistingCount()
                )
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }

        private async Task DoWork()
        {
            var runningJobs = await MainService.Scheduler.GetCurrentlyExecutingJobs();
            foreach (var context in runningJobs)
            {
                if (context.JobRunTime.TotalSeconds > AppSettings.PersistRunningJobsSpan.TotalSeconds)
                {
                    _logger.LogInformation("Persist information for job {Group}.{Name}", context.JobDetail.Key.Group, context.JobDetail.Key.Name);
                    if (context.Result is not JobExecutionMetadata metadata)
                    {
                        continue;
                    }

                    var information = metadata.GetInformation();
                    var exceptions = metadata.GetExceptionsText();

                    if (string.IsNullOrEmpty(information) && string.IsNullOrEmpty(exceptions)) { break; }

                    var log = new DbJobInstanceLog
                    {
                        InstanceId = context.FireInstanceId,
                        Information = information,
                        Exception = exceptions
                    };

                    await Policy.Handle<Exception>()
                        .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(1 * i))
                        .ExecuteAsync(() => _dal.PersistJobInstanceInformation(log));
                }
            }
        }
    }
}