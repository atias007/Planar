using Planar.API.Common.Entities;
using Planar.Common.Exceptions;
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
            info.Trigger.JobDataMap.Remove(key);
            var triggers = await BuildTriggers(info);
            await Scheduler.ScheduleJob(info.JobDetails, triggers, true);
            await Scheduler.PauseJob(info.JobKey);
        }

        public async Task UpsertData(JobOrTriggerDataRequest request, UpsertMode mode)
        {
            var info = await GetTriggerDetailsForDataCommands(request.Id, request.DataKey);
            if (info.Trigger == null || info.JobDetails == null) { return; }

            if (info.Trigger.JobDataMap.ContainsKey(request.DataKey))
            {
                if (mode == UpsertMode.Add)
                {
                    throw new RestConflictException($"data with key '{request.DataKey}' already exists");
                }

                info.Trigger.JobDataMap.Put(request.DataKey, request.DataValue);
            }
            else
            {
                if (mode == UpsertMode.Update)
                {
                    throw new RestNotFoundException($"data with key '{request.DataKey}' not found");
                }

                info.Trigger.JobDataMap.Add(request.DataKey, request.DataValue);
            }

            var triggers = await BuildTriggers(info);
            await Scheduler.ScheduleJob(info.JobDetails, triggers, true);
            await Scheduler.PauseJob(info.JobKey);
        }

        private async Task<List<ITrigger>> BuildTriggers(DataCommandDto info)
        {
            var triggers = (await Scheduler.GetTriggersOfJob(info.JobKey)).ToList();
            triggers.RemoveAll(t => TriggerKeyHelper.Equals(t.Key, info.TriggerKey));
            triggers.Add(info.Trigger);
            return triggers;
        }

        private async Task<DataCommandDto> GetTriggerDetailsForDataCommands(string triggerId, string key)
        {
#pragma warning disable IDE0017 // Simplify object initialization
            var result = new DataCommandDto();
#pragma warning restore IDE0017 // Simplify object initialization

            // Get Trigger
            result.TriggerKey = await TriggerKeyHelper.GetTriggerKey(triggerId);
            result.Trigger = await TriggerKeyHelper.ValidateTriggerExists(result.TriggerKey);

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
            if (trigger == null || !trigger.JobDataMap.ContainsKey(key))
            {
                throw new RestValidationException($"{key}", $"data with Key '{key}' could not found in trigger '{triggerId}' (Name '{trigger?.Key.Name}' and Group '{trigger?.Key.Group}')");
            }
        }

        #endregion Data

        public async Task<TriggerRowDetails> Get(string triggerId)
        {
            var triggerKey = await TriggerKeyHelper.GetTriggerKey(triggerId);
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
            var triggerKey = await TriggerKeyHelper.GetTriggerKey(triggerId);
            await ValidateExistingTrigger(triggerKey, triggerId);
            ValidateSystemTrigger(triggerKey);
            await Scheduler.PauseTrigger(triggerKey);
            var success = await Scheduler.UnscheduleJob(triggerKey);
            if (!success)
            {
                throw new PlanarException($"fail to remove trigger {triggerId}");
            }
        }

        public async Task Pause(JobOrTriggerKey request)
        {
            var key = await TriggerKeyHelper.GetTriggerKey(request);
            await Scheduler.PauseTrigger(key);
        }

        public async Task Resume(JobOrTriggerKey request)
        {
            var key = await TriggerKeyHelper.GetTriggerKey(request);
            await Scheduler.ResumeTrigger(key);
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

                throw new RestValidationException(nameof(expression), error);
            }
        }

        public async Task<IEnumerable<PausedTriggerDetails>> GetPausedTriggers()
        {
            var triggers = await Scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
            var pausedKeys = triggers.Where(t => t.Group != Consts.PlanarSystemGroup && Scheduler.GetTriggerState(t).Result == TriggerState.Paused);
            var tasks = new List<Task<ITrigger>>();
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
            if (Helpers.TriggerKeyHelper.IsSystemTriggerKey(triggerKey))
            {
                throw new RestValidationException("triggerId", "forbidden: this is system trigger and it should not be modified or deleted");
            }
        }
    }
}