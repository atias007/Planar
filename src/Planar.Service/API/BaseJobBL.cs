using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.Audit;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Quartz;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Planar.Service.API;

public class BaseJobBL<TDomain, TData>(IServiceProvider serviceProvider) : BaseLazyBL<TDomain, TData>(serviceProvider)
    where TData : BaseDataLayer

{
    protected struct DataCommandDto
    {
        public TriggerKey TriggerKey { get; set; }

        public ITrigger Trigger { get; set; }

        public JobKey JobKey { get; set; }

        public IJobDetail? JobDetails { get; set; }
    }

    protected void AuditJobsSafe(string description)
    {
        var audit = new AuditMessage
        {
            Description = description,
        };

        AuditInnerSafe(audit);
    }

    protected void AuditJobSafe(JobKey jobKey, string description, object? additionalInfo = null)
    {
        var audit = new AuditMessage
        {
            JobKey = jobKey,
            Description = description,
            AdditionalInfo = additionalInfo
        };

        AuditInnerSafe(audit);
    }

    protected void AuditTriggerSafe(TriggerKey triggerKey, string description, object? additionalInfo = null, bool addTriggerInfo = false)
    {
        var audit = new AuditMessage
        {
            TriggerKey = triggerKey,
            Description = description,
            AdditionalInfo = additionalInfo,
            AddTriggerInfo = addTriggerInfo
        };

        AuditInnerSafe(audit);
    }

    private void AuditInnerSafe(AuditMessage audit)
    {
        try
        {
            var context = Resolve<IHttpContextAccessor>();
            var claims = context?.HttpContext?.User?.Claims;
            audit.Claims = claims;
            var producer = Resolve<AuditProducer>();
            producer.Publish(audit);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "fail to publish job/trigger audit message. the message: {@Message}", audit);
        }
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
            .Select(TriggerHelper.GetKeyTitle)
            .ToList();

        if (notPaused.Count != 0)
        {
            var message = string.Join(',', notPaused);
            throw new RestValidationException("triggers", $"the following job triggers are not in pause state: {message}. pause the job before make any update");
        }
    }

    protected async Task ValidateJobNotRunning(JobKey jobKey)
    {
        var isRunning = await SchedulerUtil.IsJobRunning(jobKey);
        if (AppSettings.Cluster.Clustering)
        {
            isRunning = isRunning && await ClusterUtil.IsJobRunning(jobKey);
        }

        if (isRunning)
        {
            var title = KeyHelper.GetKeyTitle(jobKey);
            throw new RestValidationException($"{title}", $"job with key '{title}' is currently running");
        }
    }
}