using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Exceptions;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Planar.Service.API.JobDomain;

namespace Planar.Service.API
{
    public class TriggerDomain : BaseJobBL<TriggerDomain>
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
            var result = new DataCommandDto();

            // Get Trigger
            result.TriggerKey = await TriggerKeyHelper.GetTriggerKey(triggerId);
            result.Trigger = await Scheduler.GetTrigger(result.TriggerKey);
            if (result.Trigger == null) { return result; }

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
            if (trigger == null || trigger.JobDataMap.ContainsKey(key) == false)
            {
                throw new RestValidationException($"{key}", $"data with Key '{key}' could not found in trigger '{triggerId}' (Name '{trigger?.Key.Name}' and Group '{trigger?.Key.Group}')");
            }
        }

        #endregion Data

        public async Task<TriggerRowDetails> Get(string triggerId)
        {
            var triggerKey = await TriggerKeyHelper.GetTriggerKey(triggerId);
            ValidateExistingTrigger(triggerKey, triggerId);
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
            ValidateExistingTrigger(triggerKey, triggerId);
            ValidateSystemTrigger(triggerKey);
            await Scheduler.PauseTrigger(triggerKey);
            var success = await Scheduler.UnscheduleJob(triggerKey);
            if (success == false)
            {
                throw new ApplicationException("Fail to remove trigger");
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

        private async Task<TriggerRowDetails> GetTriggerDetails(TriggerKey triggerKey)
        {
            var result = new TriggerRowDetails();
            var trigger = await Scheduler.GetTrigger(triggerKey);

            if (trigger is ISimpleTrigger t1)
            {
                result.SimpleTriggers.Add(MapSimpleTriggerDetails(t1));
            }
            else
            {
                if (trigger is ICronTrigger t2)
                {
                    result.CronTriggers.Add(MapCronTriggerDetails(t2));
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
                    result.SimpleTriggers.Add(MapSimpleTriggerDetails(t1));
                }
                else
                {
                    if (t is ICronTrigger t2)
                    {
                        result.CronTriggers.Add(MapCronTriggerDetails(t2));
                    }
                }
            }

            return result;
        }

        private static void ValidateSystemTrigger(TriggerKey triggerKey)
        {
            if (triggerKey.Group == Consts.PlanarSystemGroup)
            {
                throw new RestValidationException("triggerId", "Forbidden: this is system trigger and it should not be modified or deleted");
            }
        }

        private SimpleTriggerDetails MapSimpleTriggerDetails(ISimpleTrigger source)
        {
            var result = new SimpleTriggerDetails();
            MapTriggerDetails(source, result);
            result.RepeatCount = source.RepeatCount;
            result.RepeatInterval =
                source.RepeatInterval.TotalHours < 24 ?
                $"{source.RepeatInterval:hh\\:mm\\:ss}" :
                $"{source.RepeatInterval:\\(d\\)\\ hh\\:mm\\:ss}";

            result.TimesTriggered = source.TimesTriggered;
            result.MisfireBehaviour = GetMisfireInstructionNameForSimpleTrigger(source.MisfireInstruction);
            return result;
        }

        private void MapTriggerDetails(ITrigger source, TriggerDetails target)
        {
            target.CalendarName = source.CalendarName;
            if (TimeSpan.TryParse(Convert.ToString(source.JobDataMap[Consts.RetrySpan]), out var span))
            {
                target.RetrySpan = span;
            }

            target.Description = source.Description;
            target.End = source.EndTimeUtc?.LocalDateTime;
            target.Start = source.StartTimeUtc.LocalDateTime;
            target.FinalFire = source.FinalFireTimeUtc?.LocalDateTime;
            target.Group = source.Key.Group;
            target.MayFireAgain = source.GetMayFireAgain();
            target.Name = source.Key.Name;
            target.NextFireTime = source.GetNextFireTimeUtc()?.LocalDateTime;
            target.PreviousFireTime = source.GetPreviousFireTimeUtc()?.LocalDateTime;
            target.Priority = source.Priority;
            target.DataMap = Global.ConvertDataMapToDictionary(source.JobDataMap);
            target.State = Scheduler.GetTriggerState(source.Key).Result.ToString();
            target.Id = TriggerKeyHelper.GetTriggerId(source);

            if (source.Key.Group == Consts.RecoveringJobsGroup)
            {
                target.Id = Consts.RecoveringJobsGroup;
            }
        }

        private CronTriggerDetails MapCronTriggerDetails(ICronTrigger source)
        {
            var result = new CronTriggerDetails();
            MapTriggerDetails(source, result);
            result.CronExpression = source.CronExpressionString;
            result.MisfireBehaviour = GetMisfireInstructionNameForCronTrigger(source.MisfireInstruction);
            return result;
        }

        private static string GetMisfireInstructionNameForSimpleTrigger(int value)
        {
            return value switch
            {
                -1 => "Ignore Misfire Policy",
                0 => "Instruction Not Set",
                1 => "Fire Now",
                2 => "Now With Existing Repeat Count",
                3 => "Now With Remaining Repeat Count",
                4 => "Next With Remaining Count",
                5 => "Next With Existing Count",
                _ => "Unknown",
            };
        }

        private static string GetMisfireInstructionNameForCronTrigger(int value)
        {
            return value switch
            {
                -1 => "Ignore Misfire Policy",
                0 => "Instruction Not Set",
                1 => "Fire Once Now",
                2 => "Do Nothing",
                _ => "Unknown",
            };
        }
    }
}