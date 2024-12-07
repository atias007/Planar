using Planar.Common.Helpers;
using Planar.Service.Exceptions;
using Planar.Service.SystemJobs;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.General;

internal enum AutoResumeTypes
{
    CircuitBreaker,
    AutoResume
}

internal static class AutoResumeJobUtil
{
    public const string JobKeyName = "JobKey.Name";
    public const string JobKeyGroup = "JobKey.Group";
    public const string TriggerGroup = "Trigger.Group";
    public const string TriggerNames = "Trigger.Names";
    public const string Created = "Created";
    public const string ResumeType = "ResumeType";

    public static async Task<bool> CancelQueuedResumeJob(IScheduler scheduler, JobKey jobKey)
    {
        var triggerKey = new TriggerKey($"Resume.{jobKey}", Consts.CircuitBreakerTriggerGroup);
        var trigger = await scheduler.GetTrigger(triggerKey);
        if (trigger == null) { return false; }
        var result = await scheduler.UnscheduleJob(trigger.Key);
        return result;
    }

    public static async Task QueueResumeJob(IScheduler scheduler, IJobDetail jobDetail, DateTime resumeDate, bool allTriggers = false)
    {
        var span = resumeDate - DateTime.Now;
        await QueueResumeJob(scheduler, jobDetail, span, allTriggers);
    }

    public static async Task QueueResumeJob(IScheduler scheduler, IJobDetail jobDetail, TimeSpan span, AutoResumeTypes resumeType)
    {
        // validation
        if (span == TimeSpan.Zero) { return; }

        // get job
        var jobKey = new JobKey(typeof(CircuitBreakerJob).Name, Consts.PlanarSystemGroup);
        var job = await scheduler.GetJobDetail(jobKey) ?? throw new JobNotFoundException(jobKey);

        // get triggers
        var allTriggers = resumeType == AutoResumeTypes.AutoResume;
        var activeTriggers = await GetActiveTriggers(scheduler, jobDetail, allTriggers);
        if (!activeTriggers.Any()) { return; }

        // set variables
        var triggerGroup = activeTriggers.First().Group;
        var triggerNames = activeTriggers.Select(t => t.Name);

        // create the resume trigger
        var triggerKey = new TriggerKey($"Resume.{jobDetail.Key}", Consts.CircuitBreakerTriggerGroup);
        var triggerId = ServiceUtil.GenerateId();
        var key = jobDetail.Key;
        var dueDate = DateTime.Now.Add(span);
        var newTrigger = TriggerBuilder.Create()
             .WithIdentity(triggerKey)
             .UsingJobData(Consts.TriggerId, triggerId)
             .UsingJobData(ResumeType, resumeType.ToString())
             .UsingJobData(JobKeyName, key.Name)
             .UsingJobData(JobKeyGroup, key.Group)
             .UsingJobData(TriggerGroup, triggerGroup)
             .UsingJobData(TriggerNames, string.Join(',', triggerNames))
             .UsingJobData(Created, DateTime.Now.ToString())
             .StartAt(dueDate)
             .WithSimpleSchedule(b =>
             {
                 b.WithRepeatCount(0)
                 .WithMisfireHandlingInstructionFireNow();
             })
             .ForJob(job);

        // Schedule Trigger
        await scheduler.ScheduleJob(newTrigger.Build());
    }

    private async static Task<IEnumerable<TriggerKey>> GetActiveTriggers(IScheduler scheduler, IJobDetail jobDetail, bool allTriggers)
    {
        var triggers = await scheduler.GetTriggersOfJob(jobDetail.Key);
        if (allTriggers) { return triggers.Select(t => t.Key); }
        var triggersStates = triggers.Select(async t => new { t.Key, State = await scheduler.GetTriggerState(t.Key) });
        var activeTriggers = triggersStates.Where(t => TriggerHelper.IsActiveState(t.Result.State)).Select(t => t.Result.Key);
        return activeTriggers;
    }
}