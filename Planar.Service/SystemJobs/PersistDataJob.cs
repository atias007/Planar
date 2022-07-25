using CommonJob;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.Data;
using Polly;
using Quartz;
using System;
using System.Threading.Tasks;
using DbJobInstanceLog = Planar.Service.Model.JobInstanceLog;

namespace Planar.Service.SystemJobs
{
    [DisallowConcurrentExecution]
    public class PersistDataJob : BaseSystemJob, IJob
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
            const string description = "System job for persist information & exception from running jobs";
            var span = AppSettings.PersistRunningJobsSpan;
            await Schedule<PersistDataJob>(scheduler, description, span);
        }

        private async Task DoWork()
        {
            // TODO: Cluster Support
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