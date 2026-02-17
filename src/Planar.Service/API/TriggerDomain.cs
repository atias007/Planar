using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.MapperProfiles;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Planar.Service.API.JobDomain;

namespace Planar.Service.API;

public class TriggerDomain(IServiceProvider serviceProvider) : BaseJobBL<TriggerDomain, IJobData>(serviceProvider)
{
    #region Data

    public async Task ClearData(string id)
    {
        var info = await GetTriggerDetailsForDataCommands(id);
        if (info.Trigger == null || info.JobDetails == null) { return; }

        var validKeys = info.Trigger.JobDataMap.Keys.Where(Consts.IsDataKeyValid);
        var keyCount = validKeys.Count();
        foreach (var key in validKeys)
        {
            info.JobDetails.JobDataMap.Remove(key);
        }

        var pausedTriggers = await GetPausedTriggers(info.JobKey);
        await Scheduler.PauseJob(info.JobKey);

        try
        {
            var triggers = await BuildTriggers(info.Trigger);
            await Scheduler.ScheduleJob(info.JobDetails, triggers, true);
            AuditTriggerSafe(info.TriggerKey, $"clear trigger data. {keyCount} key(s)");
        }
        finally
        {
            await PauseTriggers(info.JobKey, pausedTriggers);
        }
    }

    public async Task PutData(JobOrTriggerDataRequest request, PutMode mode, bool skipSystemCheck = false)
    {
        var info = await GetTriggerDetailsForDataCommands(request.Id, request.DataKey, skipSystemCheck);
        ValidateMaxLength(request.DataValue, 1000, "value", string.Empty);
        if (info.Trigger == null || info.JobDetails == null) { return; }

        if (IsDataKeyExists(info.Trigger, request.DataKey))
        {
            if (mode == PutMode.Add)
            {
                throw new RestConflictException($"data with key '{request.DataKey}' already exists");
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

            var dataCount = CountUserJobDataItems(info.Trigger.JobDataMap);
            if (dataCount >= Consts.MaximumJobDataItems)
            {
                throw new RestValidationException("trigger data", $"trigger data items exceeded maximum limit of {Consts.MaximumJobDataItems}");
            }

            info.Trigger.JobDataMap.Put(request.DataKey, request.DataValue);
            AuditTriggerSafe(info.TriggerKey, GetTriggerAuditDescription("add", request.DataKey), new { value = request.DataValue?.Trim() });
        }

        var pausedTriggers = await GetPausedTriggers(info.JobKey);
        await Scheduler.PauseJob(info.JobKey);

        try
        {
            var triggers = await BuildTriggers(info.Trigger);
            await Scheduler.ScheduleJob(info.JobDetails, triggers, true);
        }
        finally
        {
            await PauseTriggers(info.JobKey, pausedTriggers);
        }
    }

    public async Task RemoveData(string id, string key)
    {
        var info = await GetTriggerDetailsForDataCommands(id, key);
        if (info.Trigger == null || info.JobDetails == null) { return; }

        ValidateDataKeyExists(info.Trigger, key, id);

        var auditValue = PlanarConvert.ToString(info.Trigger.JobDataMap[key]);
        info.Trigger.JobDataMap.Remove(key);

        var pausedTriggers = await GetPausedTriggers(info.JobKey);
        await Scheduler.PauseJob(info.JobKey);

        try
        {
            var triggers = await BuildTriggers(info.Trigger);
            await Scheduler.ScheduleJob(info.JobDetails, triggers, true);
            AuditTriggerSafe(info.TriggerKey, GetTriggerAuditDescription("remove", key), new { value = auditValue?.Trim() }, addTriggerInfo: true);
        }
        finally
        {
            await PauseTriggers(info.JobKey, pausedTriggers);
        }
    }

    private static string GetTriggerAuditDescription(string operation, string key)
    {
        return $"{operation} trigger data with key '{key}' ({{{{TriggerId}}}})";
    }

    private static bool IsDataKeyExists(ITrigger trigger, string key)
    {
        if (trigger == null) { return false; }
        var result = trigger.JobDataMap.Any(k => string.Equals(key, k.Key, StringComparison.OrdinalIgnoreCase));
        return result;
    }

    private static void ValidateDataKeyExists(ITrigger trigger, string key, string triggerId)
    {
        if (!IsDataKeyExists(trigger, key))
        {
            throw new RestValidationException($"{key}", $"data with Key '{key}' could not found in trigger '{triggerId}' (name '{trigger?.Key.Name}')");
        }
    }

    private async Task<List<ITrigger>> BuildTriggers(ITrigger trigger)
    {
        var triggers = (await Scheduler.GetTriggersOfJob(trigger.JobKey)).ToList();
        triggers.RemoveAll(t => TriggerHelper.Equals(t.Key, trigger.Key));
        triggers.Add(trigger);
        return triggers;
    }

    private async Task<DataCommandDto> GetTriggerDetailsForDataCommands(string triggerId, string? key = null, bool skipSystemCheck = false)
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
        if (key != null)
        {
            ValidateSystemDataKey(key);
        }

        if (!skipSystemCheck)
        {
            ValidateSystemTrigger(trigger);
            ValidateSystemJob(result.JobKey);
        }

        await ValidateTriggerNotRunning(trigger.Key);

        return result;
    }

    #endregion Data

    public static string GetCronDescription(string expression)
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

    public async Task Delete(string triggerId)
    {
        var trigger = await GetTriggerById(triggerId);
        ValidateSystemTrigger(trigger);
        await Scheduler.PauseTrigger(trigger.Key);
        var details = GetTriggerDetails(trigger);
        var success = await Scheduler.UnscheduleJob(trigger.Key);
        if (!success)
        {
            throw new PlanarException($"fail to remove trigger {triggerId}");
        }

        // audit
        object? obj = details.SimpleTriggers.Count != 0 ? details.SimpleTriggers[0] : details.CronTriggers.FirstOrDefault();
        AuditJobSafe(trigger.JobKey, $"trigger '{trigger.Key.Name}' removed", obj);
    }

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

    public async Task<IEnumerable<string>> GetAllIds()
    {
        var keys = await Scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
        var ids = keys.Select(async k => await Scheduler.GetTrigger(k));
        await Task.WhenAll(ids);
        var triggers = ids.Select(t => t.Result).Where(t => t != null);
        var triggersIds = triggers
            .Select(t => TriggerHelper.GetTriggerId(t) ?? string.Empty)
            .Where(id => !string.IsNullOrWhiteSpace(id));
        return triggersIds;
    }

    public async Task<IEnumerable<PausedTriggerDetails>> GetPausedTriggers()
    {
        var triggers = await Scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
        var pausedKeys = triggers.Where(t => t.Group != Consts.PlanarSystemGroup && Scheduler.GetTriggerState(t).Result == TriggerState.Paused);
        var tasks = new List<Task<ITrigger?>>();
        foreach (var k in pausedKeys)
        {
            if (TriggerHelper.IsSystemTriggerKey(k)) { continue; }
            tasks.Add(Scheduler.GetTrigger(k));
        }

        await Task.WhenAll(tasks);
        var pausedTriggers = tasks.Select(t => t.Result);
        var result = Mapper.Map<List<PausedTriggerDetails>>(pausedTriggers);
        return result;
    }

    public async Task Pause(JobOrTriggerKey request)
    {
        var trigger = await GetTriggerById(request.Id);
        await Scheduler.PauseTrigger(trigger.Key);

        // audit
        AuditJobSafe(trigger.JobKey, $"trigger '{trigger.Key.Name}' paused");
    }

    public async Task Resume(JobOrTriggerKey request)
    {
        var trigger = await GetTriggerById(request.Id);
        await Scheduler.ResumeTrigger(trigger.Key);

        // audit
        AuditJobSafe(trigger.JobKey, $"trigger '{trigger.Key.Name}' resume");

        // cancel auto resume when trigger is resumed and job is fully active
        var resume = await AutoResumeJobUtil.GetAutoResumeDate(Scheduler, trigger.JobKey);
        if (resume == null) { return; }
        var mode = await JobHelper.GetJobActiveMode(Scheduler, trigger.JobKey);
        if (mode == JobActiveMembers.Active)
        {
            await CancelQueuedResumeJob(trigger.JobKey);
        }
    }

    public async Task UpdateCron(UpdateCronRequest request)
    {
        var trigger = await GetTriggerById(request.Id);
        ValidateSystemTrigger(trigger);
        var cronTrigger = ValidateCronTrigger(trigger, request);
        if (cronTrigger.CronExpressionString == request.CronExpression) { return; }

        var sourceCron = cronTrigger.CronExpressionString;
        cronTrigger.CronExpressionString = request.CronExpression;

        try
        {
            await Scheduler.RescheduleJob(trigger.Key, cronTrigger);
        }
        catch (Exception ex)
        {
            ValidateTriggerNeverFire(ex, request.Id);
            throw;
        }

        // audit
        var obj = new { from = sourceCron, to = request.CronExpression };
        AuditJobSafe(trigger.JobKey, $"update trigger '{trigger.Key.Name}' cron expression", obj);
    }

    public async Task UpdateInterval(UpdateIntervalRequest request)
    {
        var trigger = await GetTriggerById(request.Id);
        ValidateSystemTrigger(trigger);
        var simpleTrigger = ValidateSimpleTrigger(trigger, request);
        ValidateIntervalForTrigger(simpleTrigger, request.Interval);
        if (simpleTrigger.RepeatInterval == request.Interval) { return; }

        var sourceInterval = simpleTrigger.RepeatInterval;
        simpleTrigger.RepeatInterval = request.Interval;

        try
        {
            await Scheduler.RescheduleJob(trigger.Key, simpleTrigger);
        }
        catch (Exception ex)
        {
            ValidateTriggerNeverFire(ex, request.Id);
            throw;
        }

        // audit
        var obj = new { from = FormatTimeSpan(sourceInterval), to = FormatTimeSpan(request.Interval) };
        AuditJobSafe(trigger.JobKey, $"update trigger '{trigger.Key.Name}' interval", obj);
    }

    public async Task UpdateTimeout(UpdateTimeoutRequest request)
    {
        // Get Trigger & Job
        var trigger = await GetTriggerById(request.Id);
        var jobDetails = await Scheduler.GetJobDetail(trigger.JobKey);

        // Validations
        if (jobDetails == null) { return; }
        ValidateSystemTrigger(trigger);
        await ValidateJobNotRunning(trigger.JobKey);

        var timeout = TriggerHelper.GetTimeout(trigger);
        if (timeout == request.Timeout) { return; }
        TriggerHelper.SetTimeout(trigger, request.Timeout);

        var pausedTriggers = await GetPausedTriggers(jobDetails.Key);
        await Scheduler.PauseJob(jobDetails.Key);

        try
        {
            var triggers = await BuildTriggers(trigger);
            await Scheduler.ScheduleJob(jobDetails, triggers, true);
        }
        finally
        {
            await PauseTriggers(jobDetails.Key, pausedTriggers);
        }

        // audit
        var obj = new { from = FormatTimeSpan(timeout), to = FormatTimeSpan(request.Timeout) };
        AuditJobSafe(trigger.JobKey, $"update trigger '{trigger.Key.Name}' timeout", obj);
    }

    private static string FormatTimeSpan(TimeSpan? value)
    {
        if (value == null) { return string.Empty; }
        return $"{value:\\(d\\)\\ hh\\:mm\\:ss}";
    }

    private static string? GetTriggerId(ITrigger? trigger)
    {
        if (trigger == null)
        {
            throw new PlanarJobException("trigger is null at TriggerHelper.GetTriggerId(ITrigger)");
        }

        return TriggerHelper.GetTriggerId(trigger);
    }

    private static ICronTrigger ValidateCronTrigger(ITrigger trigger, UpdateCronRequest request)
    {
        if (trigger is not ICronTrigger cronTrigger)
        {
            throw new RestValidationException("triggerId", $"trigger '{request.Id}' is not cron trigger");
        }

        return cronTrigger;
    }

    private static void ValidateIntervalForTrigger(ISimpleTrigger trigger, TimeSpan interval)
    {
        if (trigger.RepeatCount >= 0 && interval.TotalSeconds < 60)
        {
            throw new RestValidationException("interval", $"interval has invalid value. interval must be greater or equals to 1 minute");
        }
    }

    private static ISimpleTrigger ValidateSimpleTrigger(ITrigger trigger, UpdateIntervalRequest request)
    {
        if (trigger is not ISimpleTrigger simpleTrigger)
        {
            throw new RestValidationException("triggerId", $"trigger '{request.Id}' is not simple trigger");
        }

        return simpleTrigger;
    }

    private static void ValidateSystemTrigger(ITrigger trigger)
    {
        if (TriggerHelper.IsSystemTriggerKey(trigger.Key))
        {
            throw new RestValidationException("triggerId", "forbidden: this is system trigger and it should not be modified or deleted");
        }

        ValidateSystemJob(trigger.JobKey);
    }

    private async Task<bool> CancelQueuedResumeJob(JobKey jobKey)
    {
        var cancelAutoResume = await AutoResumeJobUtil.CancelQueuedResumeJob(Scheduler, jobKey);
        if (cancelAutoResume) { AuditJobSafe(jobKey, "cancel existing auto resume"); }
        return cancelAutoResume;
    }

    private async Task<ITrigger> GetTriggerById(string? triggerId)
    {
        TriggerKey? key = null;
        if (string.IsNullOrWhiteSpace(triggerId))
        {
            throw new RestValidationException("triggerId", "triggerId is required");
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
}