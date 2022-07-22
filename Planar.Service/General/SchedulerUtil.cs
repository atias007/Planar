using Quartz;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.General
{
    public static class SchedulerUtil
    {
        public static async Task Start(CancellationToken cancellationToken = default)
        {
            await MainService.Scheduler.Start(cancellationToken);
        }

        public static async Task Stop(CancellationToken cancellationToken = default)
        {
            await MainService.Scheduler.Standby(cancellationToken);
        }

        public static async Task<bool> IsJobRunning(JobKey jobKey, CancellationToken cancellationToken = default)
        {
            var allRunning = await MainService.Scheduler.GetCurrentlyExecutingJobs(cancellationToken);
            var result = allRunning.AsQueryable().Any(c => c.JobDetail.Key.Name == jobKey.Name && c.JobDetail.Key.Group == jobKey.Group);
            return result;
        }
    }
}