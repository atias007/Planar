using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Planner.Common;
using Planner.Service.Data;
using Polly;
using Quartz;
using System;
using System.Threading.Tasks;
using DbJobInstanceLog = Planner.Service.Model.JobInstanceLog;
using Planner.Service.General;

namespace Planner.Service.SystemJobs
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
                _logger.LogError(ex, "Fail to persist data {@message}", ex.Message);
                return Task.CompletedTask;
            }
        }

        public static async Task Schedule(IScheduler scheduler)
        {
            var jobKey = new JobKey(nameof(PersistDataJob), Consts.PlannerSystemGroup);
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
            }

            if (job == null)
            {
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
                        .WithMisfireHandlingInstructionIgnoreMisfires()
                    )
                    .Build();

                await scheduler.ScheduleJob(job, trigger);
            }
        }

        private async Task DoWork()
        {
            var runningJobs = await MainService.Scheduler.GetCurrentlyExecutingJobs();
            foreach (var job in runningJobs)
            {
                if (job.JobRunTime.TotalSeconds > AppSettings.PersistRunningJobsSpan.TotalSeconds)
                {
                    _logger.LogInformation("Persist information for job {@Group}.{@Name}", job.JobDetail.Key.Group, job.JobDetail.Key.Name);
                    var information = JobExecutionMetadataUtil.GetInformation(job);
                    var exceptions = JobExecutionMetadataUtil.GetExceptionsText(job);

                    if (string.IsNullOrEmpty(information) && string.IsNullOrEmpty(exceptions)) { break; }

                    var log = new DbJobInstanceLog
                    {
                        InstanceId = job.FireInstanceId,
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