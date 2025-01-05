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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;

namespace Planar.Service.API;

public class BaseJobBL<TDomain, TData>(IServiceProvider serviceProvider) : BaseLazyBL<TDomain, TData>(serviceProvider)
    where TData : IBaseDataLayer

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
            audit.CliUserName = ExtractRequestHeader(context?.HttpContext, Consts.CliUserName);
            audit.CliUserDomainName = ExtractRequestHeader(context?.HttpContext, Consts.CliUserDomainName);
            var producer = Resolve<AuditProducer>();
            producer.Publish(audit);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "fail to publish job/trigger audit message. the message: {@Message}", audit);
        }
    }

    private string? ExtractRequestHeader(HttpContext? context, string key)
    {
        try
        {
            if (context == null) { return null; }
            if (!context.Request.Headers.TryGetValue(key, out var result)) { return null; }
            return result.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Fail to extract key {Key} from http request header", key);
            return null;
        }
    }

    protected static void ValidateSystemJob(JobKey jobKey)
    {
        if (JobKeyHelper.IsSystemJobKey(jobKey))
        {
            throw new RestValidationException("key", "forbidden: this is system job and it should not be modified");
        }
    }

    protected static void ValidateSystemGroup(string group)
    {
        if (JobKeyHelper.IsSystemJobGroup(group))
        {
            throw new RestValidationException("group", "forbidden: this is system group and it should not be modified");
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

    protected static int CountUserJobDataItems(JobDataMap dataMap)
    {
        return dataMap.Count(d => Consts.IsDataKeyValid(d.Key));
    }

    protected static int CountUserJobDataItems(Dictionary<string, string?> dataMap)
    {
        return dataMap.Count(d => Consts.IsDataKeyValid(d.Key));
    }

    protected static void ValidateSystemDataKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new RestValidationException("key", "key is required");
        }

        ValidateRange(key, 1, 100, "key", string.Empty);

        if (!Consts.IsDataKeyValid(key))
        {
            throw new RestValidationException("key", $"forbidden: '{key}' is system data key and it should not be modified");
        }
    }

    protected async Task ValidateTriggerPausedOrNormal(ITrigger trigger)
    {
        var state = await Scheduler.GetTriggerState(trigger.Key);
        if (state != TriggerState.Paused && state != TriggerState.Normal)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"fail to execute operation");
            sb.AppendLine();
            sb.AppendLine("trigger is not in Paused or Normal state");
            sb.AppendLine($"currently state is: {state}");
            sb.AppendLine("pause the trigger or the job or wait for Normal state before make this operation");

            var suggestion = new StringBuilder();
            var id = TriggerHelper.GetTriggerId(trigger);
            suggestion.AppendLine("use the following command to pause the trigger");
            suggestion.AppendLine($"planar-cli trigger pause {id}");
            throw new RestValidationException(
                fieldName: "triggerId",
                errorDetails: $"trigger is not in Paused or Normal state. currently state is: {state}. pause the trigger or the job or wait for Normal state before make this operation",
                clientMessage: sb.ToString(),
                suggestion: suggestion.ToString());
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
            sb.AppendLine($"fail to execute operation");
            sb.AppendLine();
            sb.AppendLine("the following triggers are not in Paused state:");
            foreach (var item in notPaused)
            {
                sb.AppendLine($" * {item}");
            }
            sb.AppendLine();
            sb.AppendLine("pause the job before make any update");

            var suggestion = new StringBuilder();
            suggestion.AppendLine("use the following command to pause the job");
            suggestion.AppendLine($"planar-cli job pause {id}");

            var cliMessage = sb.ToString();
            var suggestionMessage = suggestion.ToString();
            var message = $"the following job triggers are not in Paused state: {string.Join(',', notPaused)} pause the job before make this operation";

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

    protected async Task ValidateTriggerNotRunning(TriggerKey triggerKey)
    {
        var isRunning = await SchedulerUtil.IsTriggerRunning(triggerKey);
        if (AppSettings.Cluster.Clustering)
        {
            isRunning = isRunning && await ClusterUtil.IsTriggerRunning(triggerKey);
        }

        if (isRunning)
        {
            throw new RestValidationException($"{triggerKey.Name}", $"trigger with key '{triggerKey.Name}' is currently running");
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

    #region Validation

    protected static void ValidateMaxLength(string? value, int length, string name, string parent)
    {
        if (value != null && value.Length > length)
        {
            throw new RestValidationException(name, $"{parent} {name} length is invalid. maximum length is {length}".Trim());
        }
    }

    protected static void ValidateMaxValue(int? value, int to, string name, string parent)
    {
        if (value != null && value > to)
        {
            throw new RestValidationException(name, $"{parent} {name} value is invalid. maximum value is {to}".Trim());
        }
    }

    protected static void ValidateMaxMinutes(TimeSpan? timeSpan, int to, string name, string parent)
    {
        if (timeSpan != null && timeSpan.Value.TotalMinutes > to)
        {
            throw new RestValidationException(name, $"{parent} {name} value is invalid. maximum value is {to} minutes".Trim());
        }
    }

    protected static void ValidateMinMinutes(TimeSpan? timeSpan, int from, string name, string parent)
    {
        if (timeSpan != null && timeSpan.Value.TotalMinutes < from)
        {
            throw new RestValidationException(name, $"{parent} {name} value is invalid. minimum value is {from} minutes".Trim());
        }
    }

    protected static void ValidateMinLength(string? value, int length, string name, string parent)
    {
        if (value != null && value.Length < length)
        {
            throw new RestValidationException(name, $"{parent} {name} length is invalid. minimum length is {length}".Trim());
        }
    }

    protected static void ValidateMinValue(int? value, int from, string name, string parent)
    {
        if (value != null && value < from)
        {
            throw new RestValidationException(name, $"{parent} {name} value is invalid. minimum value is {from}".Trim());
        }
    }

    protected static void ValidateRange(string? value, int from, int to, string name, string parent)
    {
        ValidateMinLength(value, from, name, parent);
        ValidateMaxLength(value, to, name, parent);
    }

    protected static void ValidateRangeValue(int? value, int from, int to, string name, string parent)
    {
        ValidateMinValue(value, from, name, parent);
        ValidateMaxValue(value, to, name, parent);
    }

    #endregion Validation
}