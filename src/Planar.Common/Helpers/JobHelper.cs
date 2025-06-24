using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Common.Helpers;
using Quartz;
using System.Threading.Tasks;

namespace Planar.Service.API.Helpers;

public static class JobHelper
{
    public static bool IsSequenceJob(JobDataMap dataMap)
    {
        if (dataMap == null) { return false; }
        return dataMap.ContainsKey(Consts.SequenceInstanceIdDataKey);
    }

    public static string? GetSequenceTriggerId(JobDataMap dataMap)
    {
        if (dataMap == null) { return null; }
        if (dataMap.TryGetValue(Consts.SequenceTriggerIdDataKey, out var id))
        {
            return PlanarConvert.ToString(id);
        }

        return null;
    }

    public static string? GetSequenceJobKey(JobDataMap dataMap)
    {
        if (dataMap == null) { return null; }
        if (dataMap.TryGetValue(Consts.SequenceJobKeyDataKey, out var id))
        {
            return PlanarConvert.ToString(id);
        }

        return null;
    }

    public static string? GetSequenceInstanceId(JobDataMap dataMap)
    {
        if (dataMap == null) { return null; }
        if (dataMap.TryGetValue(Consts.SequenceInstanceIdDataKey, out var id))
        {
            return PlanarConvert.ToString(id);
        }

        return null;
    }

    public static string? GetJobAuthor(IJobDetail job)
    {
        if (job == null)
        {
            throw new PlanarException("job is null at JobHelper.GetJobAuthor(IJobDetail)");
        }

        if (job.JobDataMap.TryGetValue(Consts.Author, out var id))
        {
            return PlanarConvert.ToString(id);
        }

        return null;
    }

    public static JobCircuitBreakerMetadata? GetJobCircuitBreaker(IJobDetail job)
    {
        if (job == null)
        {
            throw new PlanarException("job is null at JobHelper.GetJobCircuitBreaker(IJobDetail)");
        }

        if (!job.JobDataMap.TryGetValue(Consts.CircuitBreaker, out var circuitBreakerObj))
        {
            return null;
        }

        var circuitBreakerText = PlanarConvert.ToString(circuitBreakerObj);
        if (string.IsNullOrWhiteSpace(circuitBreakerText)) { return null; }
        var cb = JobCircuitBreakerMetadata.Parse(circuitBreakerText);
        return cb;
    }

    public static int? GetLogRetentionDays(IJobDetail job)
    {
        if (job == null)
        {
            throw new PlanarException("job is null at JobHelper.GetLogRetentionDays(IJobDetail)");
        }

        if (job.JobDataMap.TryGetValue(Consts.LogRetentionDays, out var id) && int.TryParse(PlanarConvert.ToString(id), out var result))
        {
            return result;
        }

        return null;
    }

    public static string? GetJobId(IJobDetail? job)
    {
        if (job == null)
        {
            throw new PlanarException("job is null at JobKeyHelper.GetJobId(IJobDetail)");
        }

        if (job.JobDataMap.TryGetValue(Consts.JobId, out var id))
        {
            return PlanarConvert.ToString(id);
        }

        return null;
    }

    public static string GetKeyTitle(IJobDetail jobDetail)
    {
        var title = KeyHelper.GetKeyTitle(jobDetail.Key);
        return title;
    }

    public static async Task<JobActiveMembers> GetJobActiveMode(IScheduler scheduler, JobKey jobKey)
    {
        var triggers = await scheduler.GetTriggersOfJob(jobKey);
        if (triggers == null || triggers.Count == 0) { return JobActiveMembers.NoTrigger; }

        var hasActive = false;
        var hasInactive = false;
        foreach (var t in triggers)
        {
            if (t.Key.Group == Consts.RecoveringJobsGroup) { continue; }
            if (t.Key.Group == Consts.ManualTriggerId) { continue; }
            var active = await IsTriggerActive(scheduler, t);
            if (active)
            {
                hasActive = true;
            }
            else
            {
                hasInactive = true;
            }

            if (hasActive && hasInactive) { break; }
        }

        if (hasActive && hasInactive) { return JobActiveMembers.PartiallyActive; }
        if (hasActive) { return JobActiveMembers.Active; }
        if (hasInactive) { return JobActiveMembers.Inactive; }

        return JobActiveMembers.Active;
    }

    private static async Task<bool> IsTriggerActive(IScheduler scheduler, ITrigger trigger)
    {
        var state = await scheduler.GetTriggerState(trigger.Key);
        return TriggerHelper.IsActiveState(state);
    }
}