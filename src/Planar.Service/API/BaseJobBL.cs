using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Planar.Service.Audit;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Quartz;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            // build CLI message
            var details = await Scheduler.GetJobDetail(jobKey);
            var id = JobHelper.GetJobId(details);
            var sb = new StringBuilder();
            sb.AppendLine($"could not pause job {details?.Key} ({id})");
            sb.AppendLine();
            sb.AppendLine("the following triggers are not in pause state:");
            foreach (var item in notPaused)
            {
                sb.AppendLine($" * {item}");
            }
            sb.AppendLine();
            sb.AppendLine("pause the job before make any update");

            var suggestion = new StringBuilder();
            suggestion.AppendLine("use the following command to pause the job");
            suggestion.AppendLine($"planar job pasue {id}");

            var cliMessage = sb.ToString();
            var suggestionMessage = suggestion.ToString();
            var message = $"the following job triggers are not in pause state: {string.Join(',', notPaused)} pause the job before make any update";

            throw new RestValidationException("triggers", message, cliMessage, suggestionMessage);
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

    protected static void ValidateTriggerNeverFire(Exception ex, string? triggerId = null)
    {
        if (ex is not SchedulerException) { return; }
        var message = ex.Message;
        const string pattern = "(the given trigger).*(will never fire)";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

        if (regex.IsMatch(ex.Message))
        {
            triggerId ??= GetTriggerIdFromErrorMessage(message);
            if (string.IsNullOrWhiteSpace(triggerId))
            {
                throw new RestValidationException("trigger", "trigger will never fire. check trigger start/end times, cron expression, calendar and working hours configuration");
            }
            else
            {
                throw new RestValidationException("trigger", $"trigger with id '{triggerId}' will never fire. check trigger start/end times, cron expression, calendar and working hours configuration");
            }
        }
    }

    private static string? GetTriggerIdFromErrorMessage(string message)
    {
        var parts = message.Split('\'');
        if (parts.Length != 3) { return null; }
        var triggerId = parts[1];
        if (triggerId.Contains('.'))
        {
            triggerId = triggerId.Split('.')[1];
        }

        return triggerId;
    }
}