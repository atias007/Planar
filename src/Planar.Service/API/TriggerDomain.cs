using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.MapperProfiles;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Planar.Service.API.JobDomain;

namespace Planar.Service.API;

public class TriggerDomain(IServiceProvider serviceProvider) : BaseJobBL<TriggerDomain, JobData>(serviceProvider)
{
    #region Data

    public async Task RemoveData(string id, string key)
    {
        var info = await GetTriggerDetailsForDataCommands(id, key);
        if (info.Trigger == null || info.JobDetails == null) { return; }

        ValidateDataKeyExists(info.Trigger, key, id);
        var auditValue = PlanarConvert.ToString(info.Trigger.JobDataMap[key]);
        info.Trigger.JobDataMap.Remove(key);
        var triggers = await BuildTriggers(info);
        await Scheduler.ScheduleJob(info.JobDetails, triggers, true);
        await Scheduler.PauseJob(info.JobKey);

        AuditTriggerSafe(info.TriggerKey, GetTriggerAuditDescription("remove", key), new { value = auditValue?.Trim() }, addTriggerInfo: true);
    }

    public async Task PutData(JobOrTriggerDataRequest request, PutMode mode, bool skipSystemCheck = false)
    {
        var info = await GetTriggerDetailsForDataCommands(request.Id, request.DataKey, skipSystemCheck);
        if (info.Trigger == null || info.JobDetails == null) { return; }

        if (IsDataKeyExists(info.Trigger, request.DataKey))
        {
            if (mode == PutMode.Add)
            {
                throw new RestConflictException($"data with key '{request.DataKey}' already exists");
            }

            if (info.Trigger.JobDataMap.Count >= Consts.MaximumJobDataItems)
            {
                throw new RestValidationException("trigger data", $"trigger data items exceeded maximum limit of {Consts.MaximumJobDataItems}");
            }

            info.Trigger.JobDataMap.Put(request.DataKey, request.DataValue);
            AuditTriggerSafe(info.TriggerKey, GetTriggerAuditDescription("update", request.DataKey), new { value = request.DataValue?.Trim() });
        }
        else
        {
            if (mode == PutMode.Update)
            {
                throw new RestNotFoundException($"data with key '{request.DataKey}' not found");
            }

            info.Trigger.JobDataMap.Put(request.DataKey, request.DataValue);
            AuditTriggerSafe(info.TriggerKey, GetTriggerAuditDescription("add", request.DataKey), new { value = request.DataValue?.Trim() });
        }

        var triggers = await BuildTriggers(info);
        await Scheduler.ScheduleJob(info.JobDetails, triggers, true);
        await Scheduler.PauseJob(info.JobKey);
    }

    private static string GetTriggerAuditDescription(string operation, string key)
    {
        return $"{operation} trigger data with key '{key}' ({{{{TriggerId}}}})";
    }

    private async Task<List<ITrigger>> BuildTriggers(DataCommandDto info)
    {
        var triggers = (await Scheduler.GetTriggersOfJob(info.JobKey)).ToList();
        triggers.RemoveAll(t => TriggerHelper.Equals(t.Key, info.TriggerKey));
        triggers.Add(info.Trigger);
        return triggers;
    }

    private async Task<DataCommandDto> GetTriggerDetailsForDataCommands(string triggerId, string key, bool skipSystemCheck = false)
    {
        var result = new DataCommandDto();

        // Get Trigger
        var trigger = await GetTriggerById(triggerId);
        result.TriggerKey = trigger.Key;
        result.Trigger = trigger;

        // Get Job
        result.JobKey = result.Trigger.JobKey;
        result.JobDetails = await Scheduler.GetJobDetail(result.JobKey);
        if (result.JobDetails == null) { return result; }

        // Validation
        ValidateSystemDataKey(key);

        if (!skipSystemCheck)
        {
            ValidateSystemTrigger(trigger);
            ValidateSystemJob(result.JobKey);
            await ValidateJobPaused(result.JobKey);
        }

        await ValidateJobNotRunning(result.JobKey);

        return result;
    }

    private static void ValidateDataKeyExists(ITrigger trigger, string key, string triggerId)
    {
        if (!IsDataKeyExists(trigger, key))
        {
            throw new RestValidationException($"{key}", $"data with Key '{key}' could not found in trigger '{triggerId}' (name '{trigger?.Key.Name}')");
        }
    }

    private static bool IsDataKeyExists(ITrigger trigger, string key)
    {
        if (trigger == null) { return false; }
        var result = trigger.JobDataMap.Any(k => string.Equals(key, k.Key, StringComparison.OrdinalIgnoreCase));
        return result;
    }

    #endregion Data

    public async Task<TriggerRowDetails> Get(string triggerId)
    {
        var trigger = await GetTriggerById(triggerId);
        var result = GetTriggerDetails(trigger);
        return result;
    }

    public async Task<TriggerRowDetails> GetByJob(string id)
    {
        var jobKey = await JobKeyHelper.GetJobKey(id);
        var result = await GetTriggersDetails(jobKey);
        return result;
    }

    public async Task Delete(string triggerId)
    {
        var trigger = await GetTriggerById(triggerId);
        ValidateSystemTrigger(trigger);
        await Scheduler.PauseTrigger(trigger.Key);
        var details = GetTriggerDetails(trigger);
        var triggerIdentifier = GetTriggerId(trigger);
        var success = await Scheduler.UnscheduleJob(trigger.Key);
        if (!success)
        {
            throw new PlanarException($"fail to remove trigger {triggerId}");
        }

        // Audit
        object? obj = details.SimpleTriggers.Count != 0 ? details.SimpleTriggers[0] : details.CronTriggers.FirstOrDefault();
        AuditJobSafe(trigger.JobKey, $"trigger {triggerIdentifier} removed", obj);
    }

    public async Task UpdateCron(UpdateCronRequest request)
    {
        var trigger = await GetTriggerById(request.Id);
        ValidateSystemTrigger(trigger);
        //// await ValidateTriggerPaused(trigger);
        var cronTrigger = ValidateCronTrigger(trigger, request);
        if (cronTrigger.CronExpressionString == request.CronExpression) { return; }

        var sourceCron = cronTrigger.CronExpressionString;
        cronTrigger.CronExpressionString = request.CronExpression;

        try
        {
            await Scheduler.RescheduleJob(trigger.Key, cronTrigger);
        }
        catch (SchedulerException ex)
        {
            const string pattern = "(the given trigger).*(will never fire)";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
            if (regex.IsMatch(ex.Message))
            {
                throw new RestValidationException("id", $"the given trigger '{request.Id}' will never fire");
            }
        }

        // Audit
        var obj = new { From = sourceCron, To = request.CronExpression, TriggerKey = cronTrigger.Key };
        AuditJobSafe(trigger.JobKey, $"update trigger {request.Id} cron expression", obj);
    }

    public async Task UpdateInterval(UpdateIntervalRequest request)
    {
        var trigger = await GetTriggerById(request.Id);
        ValidateSystemTrigger(trigger);
        //// await ValidateTriggerPaused(trigger);
        var simpleTrigger = ValidateSimpleTrigger(trigger, request);
        ValidateIntervalForTrigger(simpleTrigger, request.Interval);
        if (simpleTrigger.RepeatInterval == request.Interval) { return; }

        var sourceInterval = simpleTrigger.RepeatInterval;
        simpleTrigger.RepeatInterval = request.Interval;
        await Scheduler.RescheduleJob(trigger.Key, simpleTrigger);

        // Audit
        var obj = new { From = FormatTimeSpan(sourceInterval), To = FormatTimeSpan(request.Interval), TriggerKey = simpleTrigger.Key };
        AuditJobSafe(trigger.JobKey, $"update trigger {request.Id} interval", obj);
    }

    public async Task Pause(JobOrTriggerKey request)
    {
        var trigger = await GetTriggerById(request.Id);
        await Scheduler.PauseTrigger(trigger.Key);

        // audit
        var id = GetTriggerId(trigger);
        AuditJobSafe(trigger.JobKey, $"trigger {id} paused", new { TriggerKey = trigger.Key });
    }

    public async Task Resume(JobOrTriggerKey request)
    {
        var trigger = await GetTriggerById(request.Id);
        await Scheduler.ResumeTrigger(trigger.Key);

        // audit
        var id = GetTriggerId(trigger);
        AuditJobSafe(trigger.JobKey, $"trigger {id} resume", new { TriggerKey = trigger.Key });
    }

    public string GetCronDescription(string expression)
    {
        try
        {
            return TriggerDetailsProfile.GetCronDescription(expression);
        }
        catch (FormatException ex)
        {
            const string errorString = "Error: ";
            const string doubleSpace = "  ";
            const string singleSpace = " ";
            var error = ex.Message?
                .Replace(errorString, string.Empty)
                .Replace(doubleSpace, singleSpace)
                .ToLowerInvariant();

            throw new RestValidationException(nameof(expression), error ?? "general error");
        }
    }

    public async Task<IEnumerable<PausedTriggerDetails>> GetPausedTriggers()
    {
        var triggers = await Scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
        var pausedKeys = triggers.Where(t => t.Group != Consts.PlanarSystemGroup && Scheduler.GetTriggerState(t).Result == TriggerState.Paused);
        var tasks = new List<Task<ITrigger?>>();
        foreach (var k in pausedKeys)
        {
            tasks.Add(Scheduler.GetTrigger(k));
        }

        await Task.WhenAll(tasks);
        var pausedTriggers = tasks.Select(t => t.Result);
        var result = Mapper.Map<List<PausedTriggerDetails>>(pausedTriggers);
        return result;
    }

    private static string FormatTimeSpan(TimeSpan? value)
    {
        if (value == null) { return string.Empty; }
        return $"{value:\\(d\\)\\ hh\\:mm\\:ss}";
    }

    private static void ValidateIntervalForTrigger(ISimpleTrigger trigger, TimeSpan interval)
    {
        if (trigger.RepeatCount >= 0 && interval.TotalSeconds < 60)
        {
            throw new RestValidationException("interval", $"interval has invalid value. interval must be greater or equals to 1 minute");
        }
    }

    ////private async Task ValidateTriggerPaused(ITrigger trigger)
    ////{
    ////    var state = await Scheduler.GetTriggerState(trigger.Key);
    ////    if (state != TriggerState.Paused)
    ////    {
    ////        throw new RestValidationException("triggerId", "trigger should be paused before update cron expression");
    ////    }
    ////}

    private static ICronTrigger ValidateCronTrigger(ITrigger trigger, UpdateCronRequest request)
    {
        if (trigger is not ICronTrigger cronTrigger)
        {
            throw new RestValidationException("triggerId", $"trigger '{request.Id}' is not cron trigger");
        }

        return cronTrigger;
    }

    private static ISimpleTrigger ValidateSimpleTrigger(ITrigger trigger, UpdateIntervalRequest request)
    {
        if (trigger is not ISimpleTrigger simpleTrigger)
        {
            throw new RestValidationException("triggerId", $"trigger '{request.Id}' is not simple trigger");
        }

        return simpleTrigger;
    }

    private TriggerRowDetails GetTriggerDetails(ITrigger trigger)
    {
        var result = new TriggerRowDetails();

        if (trigger is ISimpleTrigger t1)
        {
            var simpleTrigger = Mapper.Map<SimpleTriggerDetails>(t1);
            result.SimpleTriggers.Add(simpleTrigger);
        }
        else
        {
            if (trigger is ICronTrigger t2)
            {
                var cronTrigger = Mapper.Map<CronTriggerDetails>(t2);
                result.CronTriggers.Add(cronTrigger);
            }
        }

        return result;
    }

    private async Task<TriggerRowDetails> GetTriggersDetails(JobKey jobKey)
    {
        await JobKeyHelper.ValidateJobExists(jobKey);
        var result = new TriggerRowDetails();
        var triggers = await Scheduler.GetTriggersOfJob(jobKey);

        foreach (var t in triggers)
        {
            if (t is ISimpleTrigger t1)
            {
                var simpleTrigger = Mapper.Map<SimpleTriggerDetails>(t1);
                result.SimpleTriggers.Add(simpleTrigger);
            }
            else
            {
                if (t is ICronTrigger t2)
                {
                    var cronTrigger = Mapper.Map<CronTriggerDetails>(t2);
                    result.CronTriggers.Add(cronTrigger);
                }
            }
        }

        return result;
    }

    private static void ValidateSystemTrigger(ITrigger trigger)
    {
        if (TriggerHelper.IsSystemTriggerKey(trigger.Key))
        {
            throw new RestValidationException("triggerId", "forbidden: this is system trigger and it should not be modified or deleted");
        }
    }

    private async Task<ITrigger> GetTriggerById(string triggerId)
    {
        TriggerKey? key = null;
        if (triggerId == null)
        {
            throw new RestValidationException("triggerId", "triggerId is required");
        }

        var index = triggerId.IndexOf('.');
        if (index > 0)
        {
            key = new TriggerKey(triggerId[..index], triggerId[(index + 1)..]);
        }

        var keys = await Scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
        foreach (var k in keys)
        {
            var triggerDetails = await Scheduler.GetTrigger(k);
            var id = GetTriggerId(triggerDetails);
            if (id == triggerId)
            {
                key = k;
                break;
            }
        }

        if (key == null)
        {
            throw new RestNotFoundException($"trigger with id '{triggerId}' does not exist");
        }

        var result = await Scheduler.GetTrigger(key);

        return result ?? throw new RestNotFoundException($"trigger with id '{triggerId}' does not exist");
    }

    private static string? GetTriggerId(ITrigger? trigger)
    {
        if (trigger == null)
        {
            throw new PlanarJobException("trigger is null at TriggerHelper.GetTriggerId(ITrigger)");
        }

        return TriggerHelper.GetTriggerId(trigger);
    }
}