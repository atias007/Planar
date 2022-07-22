using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.General;
using Quartz;
using System;
using System.Threading.Tasks;

namespace Planar.Service.SystemJobs
{
    [DisallowConcurrentExecution]
    public class ClusterHealthCheckJob : IJob
    {
        private readonly ILogger<ClusterHealthCheckJob> _logger;

        private readonly DataLayer _dal;

        public ClusterHealthCheckJob()
        {
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                return DoWork();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail check health of cluster: {Message}", ex.Message);
                return Task.CompletedTask;
            }
        }

        private static async Task DoWork()
        {
            var logger = Global.GetLogger<ClusterHealthCheckJob>();
            var dal = Global.ServiceProvider.GetService<DataLayer>();
            var util = new ClusterUtil(dal, logger);
            await util.HealthCheckWithUpdate();
        }

        public static async Task Schedule(IScheduler scheduler)
        {
            var jobKey = new JobKey(nameof(ClusterHealthCheckJob), Consts.PlanarSystemGroup);
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

            job = JobBuilder.Create(typeof(ClusterHealthCheckJob))
                .WithIdentity(jobKey)
                .UsingJobData(Consts.JobId, jobId)
                .WithDescription("System job for check health of all cluster nodes")
                .StoreDurably(true)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(jobKey.Name, jobKey.Group)
                .StartAt(new DateTimeOffset(DateTime.Now.Add(AppSettings.ClusterHealthCheckInterval)))
                .UsingJobData(Consts.TriggerId, triggerId)
                .WithSimpleSchedule(s => s
                    .WithInterval(AppSettings.ClusterHealthCheckInterval)
                    .RepeatForever()
                    .WithMisfireHandlingInstructionNextWithExistingCount()
                )
                .Build();

            await scheduler.ScheduleJob(job, trigger);

            if (AppSettings.Clustering == false)
            {
                await scheduler.PauseJob(jobKey);
            }
        }
    }
}