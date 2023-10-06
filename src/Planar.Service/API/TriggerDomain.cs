using Azure;
using Azure.Core;
using CommonJob;
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
using System.Threading.Tasks;
using static Planar.Service.API.JobDomain;

namespace Planar.Service.API
{
    public class TriggerDomain : BaseJobBL<TriggerDomain, JobData>
    {
        public TriggerDomain(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

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

        public async Task PutData(JobOrTriggerDataRequest request, PutMode mode)
        {
            var info = await GetTriggerDetailsForDataCommands(request.Id, request.DataKey);
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

        private async Task<DataCommandDto> GetTriggerDetailsForDataCommands(string triggerId, string key)
        {
#pragma warning disable IDE0017 // Simplify object initialization
            var result = new DataCommandDto();
#pragma warning restore IDE0017 // Simplify object initialization

            // Get Trigger
            result.TriggerKey = await GetTriggerKeyById(triggerId);
            result.Trigger = await ValidateTriggerExists(result.TriggerKey);

            // Get Job
            result.JobKey = result.Trigger.JobKey;
            result.JobDetails = await Scheduler.GetJobDetail(result.JobKey);
            if (result.JobDetails == null) { return result; }

            // Validation
            ValidateSystemTrigger(result.TriggerKey);
            ValidateSystemJob(result.JobKey);
            ValidateSystemDataKey(key);
            await ValidateJobPaused(result.JobKey);
            await ValidateJobNotRunning(result.JobKey);
            return result;
        }

        private static void ValidateDataKeyExists(ITrigger trigger, string key, string triggerId)
        {
            if (!IsDataKeyExists(trigger, key))
            {
                throw new RestValidationException($"{key}", $"data with Key '{key}' could not found in trigger '{triggerId}' (Name '{trigger?.Key.Name}'");
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
            var triggerKey = await GetTriggerKeyById(triggerId);
            await ValidateExistingTrigger(triggerKey, triggerId);
            var result = await GetTriggerDetails(triggerKey);
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
            var triggerKey = await GetTriggerKeyById(triggerId);
            await ValidateExistingTrigger(triggerKey, triggerId);
            ValidateSystemTrigger(triggerKey);
            await Scheduler.PauseTrigger(triggerKey);
            var trigger = await ValidateTriggerExists(triggerKey);
            var details = await GetTriggerDetails(triggerKey);
            var triggerIdentifier = GetTriggerId(trigger);
            var success = await Scheduler.UnscheduleJob(triggerKey);
            if (!success)
            {
                throw new PlanarException($"fail to remove trigger {triggerId}");
            }

            // Audit
            object? obj = details.SimpleTriggers.Any() ? details.SimpleTriggers[0] : details.CronTriggers.FirstOrDefault();
            AuditJobSafe(trigger.JobKey, $"trigger removed (id: {triggerIdentifier})", obj);
        }

        public async Task Pause(JobOrTriggerKey request)
        {
            var key = await GetTriggerKeyById(request.Id);
            await Scheduler.PauseTrigger(key);

            // audit
            var trigger = ValidateTriggerExists(key).Result;
            var id = GetTriggerId(trigger);
            AuditJobSafe(trigger.JobKey, $"trigger paused (id: {id})");
        }

        public async Task Resume(JobOrTriggerKey request)
        {
            var key = await GetTriggerKeyById(request.Id);
            await Scheduler.ResumeTrigger(key);

            // audit
            var trigger = ValidateTriggerExists(key).Result;
            var id = GetTriggerId(trigger);
            AuditJobSafe(trigger.JobKey, $"trigger resume (id: {id})");
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

        private async Task<TriggerRowDetails> GetTriggerDetails(TriggerKey triggerKey)
        {
            var result = new TriggerRowDetails();
            var trigger = await Scheduler.GetTrigger(triggerKey);

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

        private static void ValidateSystemTrigger(TriggerKey triggerKey)
        {
            if (TriggerHelper.IsSystemTriggerKey(triggerKey))
            {
                throw new RestValidationException("triggerId", "forbidden: this is system trigger and it should not be modified or deleted");
            }
        }

        private async Task<TriggerKey> GetTriggerKeyById(string triggerId)
        {
            TriggerKey? result = null;
            var keys = await Scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
            foreach (var k in keys)
            {
                var triggerDetails = await Scheduler.GetTrigger(k);
                var id = GetTriggerId(triggerDetails);
                if (id == triggerId)
                {
                    result = k;
                    break;
                }
            }

            if (result == null)
            {
                throw new RestNotFoundException($"trigger with id '{triggerId}' does not exist");
            }

            return result;
        }

        private async Task<ITrigger> ValidateTriggerExists(TriggerKey triggerKey)
        {
            var exists = await Scheduler.GetTrigger(triggerKey);
            return exists ?? throw new RestNotFoundException($"trigger with key '{KeyHelper.GetKeyTitle(triggerKey)}' does not exist");
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
}