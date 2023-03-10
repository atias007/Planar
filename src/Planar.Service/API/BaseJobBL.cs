using Planar.Common;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Quartz;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Planar.Service.API
{
    public class BaseJobBL<TDomain, TData> : BaseBL<TDomain, TData>
        where TData : BaseDataLayer

    {
        public BaseJobBL(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected struct DataCommandDto
        {
            public TriggerKey TriggerKey { get; set; }

            public ITrigger Trigger { get; set; }

            public JobKey JobKey { get; set; }

            public IJobDetail JobDetails { get; set; }
        }

        protected static void ValidateSystemJob(JobKey jobKey)
        {
            if (Helpers.JobKeyHelper.IsSystemJobKey(jobKey))
            {
                throw new RestValidationException("key", "forbidden: this is system job and it should not be modified");
            }
        }

        protected TransactionScope GetTransaction()
        {
            var options = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadUncommitted,
                Timeout = TimeSpan.FromSeconds(10)
            };

            var transaction = new TransactionScope(TransactionScopeOption.Required, options);
            return transaction;
        }

        protected static void ValidateSystemDataKey(string key)
        {
            if (key.StartsWith(Consts.ConstPrefix))
            {
                throw new RestValidationException("key", "forbidden: this is system data key and it should not be modified");
            }
        }

        protected async Task ValidateJobPaused(JobKey jobKey)
        {
            var triggers = await Scheduler.GetTriggersOfJob(jobKey);
            var notPaused = triggers
                .Where(t => Scheduler.GetTriggerState(t.Key).Result != TriggerState.Paused)
                .Select(t => $"{t.Key.Group}.{t.Key.Name}")
                .ToList();

            if (notPaused.Any())
            {
                var message = string.Join(',', notPaused);
                throw new RestValidationException("triggers", $"the following job triggers are not in pause state: {message}. stop the job before make any update");
            }
        }

        protected async Task ValidateJobNotRunning(JobKey jobKey)
        {
            var isRunning = await SchedulerUtil.IsJobRunning(jobKey);
            if (AppSettings.Clustering)
            {
                isRunning = isRunning && await ClusterUtil.IsJobRunning(jobKey);
            }

            if (isRunning)
            {
                throw new RestValidationException($"{jobKey.Group}.{jobKey.Name}", $"job with name: {jobKey.Name} and group: {jobKey.Group} is currently running");
            }
        }
    }
}