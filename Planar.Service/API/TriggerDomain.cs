using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Exceptions;
using Planar.Service.Model.Metadata;
using Quartz;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class TriggerDomain : BaseBL<TriggerDomain>
    {
        public TriggerDomain(ILogger<TriggerDomain> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        public async Task<TriggerRowDetails> Get(string triggerId)
        {
            var triggerKey = await TriggerKeyHelper.GetTriggerKey(triggerId);
            var result = await GetTriggerDetails(triggerKey);
            return result;
        }

        public async Task<TriggerRowDetails> GetByJob(string jobId)
        {
            var jobKey = await JobKeyHelper.GetJobKey(jobId);
            var result = await GetTriggersDetails(jobKey);
            return result;
        }

        public async Task Delete(string triggerId)
        {
            var triggerKey = await TriggerKeyHelper.GetTriggerKey(triggerId);
            ValidateSystemTrigger(triggerKey);
            await Scheduler.PauseTrigger(triggerKey);
            var success = await Scheduler.UnscheduleJob(triggerKey);
            if (success == false)
            {
                throw new ApplicationException("Fail to remove trigger");
            }
        }

        public async Task<string> Add(AddTriggerRequest request)
        {
            var metadata = JobDomain.GetJobMetadata(request.Yaml);
            JobDomain.ValidateTriggerMetadata(metadata);
            var key = await JobKeyHelper.GetJobKey(request);
            var job = await Scheduler.GetJobDetail(key);
            await BuildTriggers(Scheduler, job, metadata);
            var id = JobKeyHelper.GetJobId(job);
            return id;
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

        private static async Task<TriggerRowDetails> GetTriggerDetails(TriggerKey triggerKey)
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

        private static async Task<TriggerRowDetails> GetTriggersDetails(JobKey jobKey)
        {
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
                throw new PlanarValidationException($"Forbidden: this is system trigger and it should not be modified or deleted");
            }
        }

        private static SimpleTriggerDetails MapSimpleTriggerDetails(ISimpleTrigger source)
        {
            var result = new SimpleTriggerDetails();
            MapTriggerDetails(source, result);
            result.RepeatCount = source.RepeatCount;
            result.RepeatInterval = $"{source.RepeatInterval:hh\\:mm\\:ss}";
            result.TimesTriggered = source.TimesTriggered;
            return result;
        }

        private static void MapTriggerDetails(ITrigger source, TriggerDetails target)
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
            target.MisfireBehaviour = source.MisfireInstruction.ToString();
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

        private static CronTriggerDetails MapCronTriggerDetails(ICronTrigger source)
        {
            var result = new CronTriggerDetails();
            MapTriggerDetails(source, result);
            result.CronExpression = source.CronExpressionString;
            return result;
        }

        private static async Task BuildTriggers(IScheduler scheduler, IJobDetail quartzJob, JobMetadata job)
        {
            var quartzTriggers1 = JobDomain.BuildTriggerWithSimpleSchedule(job.SimpleTriggers);
            var quartzTriggers2 = JobDomain.BuildTriggerWithCronSchedule(job.CronTriggers);
            var allTriggers = new List<ITrigger>();
            if (quartzTriggers1 != null) allTriggers.AddRange(quartzTriggers1);
            if (quartzTriggers2 != null) allTriggers.AddRange(quartzTriggers2);

            await scheduler.ScheduleJob(quartzJob, allTriggers, true);
        }
    }
}