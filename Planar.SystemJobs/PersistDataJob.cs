using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Planner.Common;
using Planner.Service.Data;
using Polly;
using Quartz;
using System;
using System.Threading.Tasks;
using DbJobInstanceLog = Planner.Service.Model.JobInstanceLog;

namespace Planner.Service
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
                _logger.LogError("Fail to persist data", ex);
                return Task.CompletedTask;
            }
        }

        private async Task DoWork()
        {
            var runningJobs = await MainService.Scheduler.GetCurrentlyExecutingJobs();
            foreach (var job in runningJobs)
            {
                if (job.JobRunTime.TotalSeconds > AppSettings.PersistRunningJobsSpan.TotalSeconds)
                {
                    _logger.LogInformation($"Persist information for job {job.JobDetail.Key.Group}.{job.JobDetail.Key.Name}");
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