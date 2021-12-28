using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planner.Service.Data;
using Polly;
using System;
using System.Threading;
using System.Threading.Tasks;
using DbJobInstanceLog = Planner.Service.Model.JobInstanceLog;

namespace Planner.Service
{
    public class PersistDataService : IHostedService, IDisposable
    {
        private static readonly object Locker = new();
        private static bool _isrunning = false;
        private readonly ILogger<PersistDataService> _logger;

        private readonly DataLayer _dal;

        private Timer _timer = null!;

        public PersistDataService(DataLayer dal, ILogger<PersistDataService> logger)
        {
            _logger = logger;
            _dal = dal;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(SafeDoWork, null, AppSettings.PersistRunningJobsSpan, AppSettings.PersistRunningJobsSpan);

            return Task.CompletedTask;
        }

        private async void SafeDoWork(object state)
        {
            lock (Locker)
            {
                if (_isrunning == true)
                {
                    _logger.LogWarning("Persist information skipped because there is already persist process running");
                    return;
                }

                _isrunning = true;
            }

            try
            {
                await DoWork(state);
            }
            catch (Exception ex)
            {
                _logger.LogError("Fail to persist data", ex);
            }
            finally
            {
                _isrunning = false;
            }
        }

        private async Task DoWork(object state)
        {
            var runningJobs = await MainService.Scheduler.GetCurrentlyExecutingJobs();
            foreach (var job in runningJobs)
            {
                if (job.JobRunTime.TotalSeconds > AppSettings.PersistRunningJobsSpan.TotalSeconds)
                {
                    _logger.LogInformation($"Persist information for job {job.JobDetail.Key.Group}.{job.JobDetail.Key.Name}");
                    var metadata = JobExecutionMetadata.GetInstance(job);
                    var information = metadata.Information;
                    var exceptions = metadata.GetExceptionsText();

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

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Persist Data Service is stopping");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _timer?.Dispose();
        }
    }
}