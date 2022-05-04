using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Service.API.Helpers;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public partial class JobDomain : BaseBL<JobDomain>
    {
        public JobDomain(ILogger<JobDomain> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        public async Task<List<JobRowDetails>> GetAll()
        {
            var result = new List<JobRowDetails>();

            foreach (var jobKey in await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup()))
            {
                var info = await Scheduler.GetJobDetail(jobKey);

                var details = new JobRowDetails();
                MapJobRowDetails(info, details);
                result.Add(details);
            }

            result = result
                .OrderBy(r => r.Group)
                .ThenBy(r => r.Name)
                .ToList();

            return result;
        }

        public async Task<JobDetails> Get(string id)
        {
            var jobKey = await JobKeyHelper.GetJobKey(id);
            var info = await Scheduler.GetJobDetail(jobKey);

            var result = new JobDetails();
            MapJobDetails(info, result);

            var triggers = await GetTriggersDetails(jobKey);
            result.SimpleTriggers = triggers.SimpleTriggers;
            result.CronTriggers = triggers.CronTriggers;

            return result;
        }

        public async Task Remove(string id)
        {
            var jobKey = await JobKeyHelper.GetJobKey(id);
            ValidateSystemJob(jobKey);
            await Scheduler.DeleteJob(jobKey);
        }

        public async Task UpsertData(JobDataRequest request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            ValidateSystemJob(jobKey);
            var info = await Scheduler.GetJobDetail(jobKey);
            if (info != null)
            {
                await ValidateJobNotRunning(jobKey);
                await Scheduler.PauseJob(jobKey);

                if (info.JobDataMap.ContainsKey(request.DataKey))
                {
                    info.JobDataMap.Put(request.DataKey, request.DataValue);
                }
                else
                {
                    info.JobDataMap.Add(request.DataKey, request.DataValue);
                }

                var triggers = await Scheduler.GetTriggersOfJob(jobKey);
                await Scheduler.ScheduleJob(info, triggers, true);
                await Scheduler.ResumeJob(jobKey);
            }
        }

        public async Task Invoke(InvokeJobRequest request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);

            if (request.NowOverrideValue.HasValue)
            {
                var job = await Scheduler.GetJobDetail(jobKey);
                if (job != null)
                {
                    job.JobDataMap.Add(Consts.NowOverrideValue, request.NowOverrideValue.Value);
                    await Scheduler.TriggerJob(jobKey, job.JobDataMap);
                }
            }
            else
            {
                await Scheduler.TriggerJob(jobKey);
            }
        }

        private static async Task ValidateJobNotRunning(JobKey jobKey)
        {
            var allRunning = await Scheduler.GetCurrentlyExecutingJobs();
            if (allRunning.AsQueryable().Any(c => c.JobDetail.Key.Name == jobKey.Name && c.JobDetail.Key.Group == jobKey.Group))
            {
                throw new PlanarValidationException($"job with name: {jobKey.Name} and group: {jobKey.Group} is currently running");
            }
        }

        private static void MapJobRowDetails(IJobDetail source, JobRowDetails target)
        {
            target.Id = Convert.ToString(source.JobDataMap[Consts.JobId]);
            target.Name = source.Key.Name;
            target.Group = source.Key.Group;
            target.Description = source.Description;
        }

        private static void ValidateSystemJob(JobKey jobKey)
        {
            if (jobKey.Group == Consts.PlanarSystemGroup)
            {
                throw new PlanarValidationException($"Forbidden: this is system job and it should not be modified or deleted");
            }
        }

        private static void MapJobDetails(IJobDetail source, JobDetails target, JobDataMap dataMap = null)
        {
            if (dataMap == null)
            {
                dataMap = source.JobDataMap;
            }

            MapJobRowDetails(source, target);
            target.ConcurrentExecution = !source.ConcurrentExecutionDisallowed;
            target.Durable = source.Durable;
            target.RequestsRecovery = source.RequestsRecovery;
            target.DataMap = ServiceUtil.ConvertJobDataMapToDictionary(dataMap);

            if (dataMap.ContainsKey(Consts.JobTypeProperties))
            {
                var json = Convert.ToString(dataMap[Consts.JobTypeProperties]);
                if (string.IsNullOrEmpty(json) == false)
                {
                    var dict = DeserializeObject<Dictionary<string, string>>(json);
                    target.Properties = new SortedDictionary<string, string>(dict);
                }
            }
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

        private static SimpleTriggerDetails MapSimpleTriggerDetails(ISimpleTrigger source)
        {
            var result = new SimpleTriggerDetails();
            MapTriggerDetails(source, result);
            result.RepeatCount = source.RepeatCount;
            result.RepeatInterval = source.RepeatInterval;
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
            target.DataMap = source.JobDataMap
                .AsEnumerable()
                .Where(s => s.Key.StartsWith(Consts.ConstPrefix) == false && s.Key.StartsWith(Consts.QuartzPrefix) == false)
                .ToDictionary(k => k.Key, v => Convert.ToString(v.Value));
            target.State = Scheduler.GetTriggerState(source.Key).Result.ToString();
            target.Id = Convert.ToString(source.JobDataMap[Consts.TriggerId]);

            if (source.Key.Group == Consts.RecoveringJobsGroup)
            {
                target.Id = string.Empty.PadLeft(11, '-');
            }
        }

        private static CronTriggerDetails MapCronTriggerDetails(ICronTrigger source)
        {
            var result = new CronTriggerDetails();
            MapTriggerDetails(source, result);
            result.CronExpression = source.CronExpressionString;
            return result;
        }

        private static void MapJobExecutionContext(IJobExecutionContext source, RunningJobDetails target)
        {
            target.FireInstanceId = source.FireInstanceId;
            target.NextFireTime = source.NextFireTimeUtc.HasValue ? source.NextFireTimeUtc.Value.DateTime : (DateTime?)null;
            target.PreviousFireTime = source.PreviousFireTimeUtc.HasValue ? source.PreviousFireTimeUtc.Value.DateTime : (DateTime?)null;
            target.ScheduledFireTime = source.ScheduledFireTimeUtc.HasValue ? source.ScheduledFireTimeUtc.Value.DateTime : (DateTime?)null;
            target.FireTime = source.FireTimeUtc.DateTime;
            target.RunTime = source.JobRunTime;
            target.RefireCount = source.RefireCount;
            target.TriggerGroup = source.Trigger.Key.Group;
            target.TriggerName = source.Trigger.Key.Name;
            target.DataMap = ServiceUtil.ConvertJobDataMapToDictionary(source.MergedJobDataMap);
            target.TriggerId = Convert.ToString(Convert.ToString(source.Get(Consts.TriggerId)));

            if (source.Result is JobExecutionMetadata metadata)
            {
                target.EffectedRows = metadata.EffectedRows;
                target.Progress = metadata.Progress;
            }
        }
    }
}