using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Model;
using Quartz;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Planar.Service.API;

public partial class JobDomain
{
    internal async Task<bool> HasChanges(SetJobDynamicRequest request)
    {
        var jobKey = JobKeyHelper.GetJobKey(request);
        if (jobKey == null) { return false; }

        var scheduler = await GetScheduler();
        var currentJobDetails = await scheduler.GetJobDetail(jobKey);
        if (currentJobDetails == null) { return false; }
        var currentJobId = JobHelper.GetJobId(currentJobDetails);
        if (string.IsNullOrWhiteSpace(currentJobId)) { return false; }
        var currentTriggers = (await scheduler.GetTriggersOfJob(jobKey)).OrderBy(t => t.Key.ToString()).ToList();
        var dal = Resolve<IJobData>();
        (string? currentJobPropertiesYml, string? currentJobGlobalConfigKeysYml) = await dal.GetJobProperty(currentJobId);
        var currentJobDataJson = GetDataMapJson(currentJobDetails.JobDataMap);

        var newJobDetails = BuildJobDetails(request, jobKey);
        var newJobPropertiesYml = GetJopPropertiesYml(request);
        var newJobGlobalConfigKeysYml = GetJobGlobalConfigKeysYml(request);
        AddAuthor(request, newJobDetails);
        AddCircuitBreaker(request, newJobDetails);
        AddLogRetentionDays(request, newJobDetails);
        BuildJobData(request, newJobDetails);
        newJobDetails.JobDataMap.Add(Consts.JobId, currentJobId);
        var newTriggers = BuildTriggers(request, currentJobId).OrderBy(t => t.Key.ToString()).ToList();
        var newJobDataJson = GetDataMapJson(newJobDetails.JobDataMap);

        // JobDetails
        if (currentJobDetails.ConcurrentExecutionDisallowed != newJobDetails.ConcurrentExecutionDisallowed) { return true; }
        if (currentJobDetails.Description != newJobDetails.Description) { return true; }
        if (currentJobDetails.Durable != newJobDetails.Durable) { return true; }
        if (currentJobDetails.JobType.FullName != newJobDetails.JobType.FullName) { return true; }
        if (currentJobDetails.RequestsRecovery != newJobDetails.RequestsRecovery) { return true; }
        if (currentJobDetails.Key.Group != newJobDetails.Key.Group) { return true; }
        if (currentJobDetails.Key.Name != newJobDetails.Key.Name) { return true; }

        // JobData
        if (currentJobDataJson != newJobDataJson) { return true; }

        // Properties
        if (currentJobPropertiesYml != newJobPropertiesYml) { return true; }

        // GlobalConfigKeys
        if (currentJobGlobalConfigKeysYml != newJobGlobalConfigKeysYml) { return true; }

        // Trigges
        if (currentTriggers.Count != newTriggers.Count) { return true; }

        return HasChangesInTriggers(currentTriggers, newTriggers);
    }

#pragma warning disable S3776 // Cognitive Complexity of methods should not be too high

    internal static bool HasChangesInTriggers(List<ITrigger> currentTriggers, List<ITrigger> newTriggers)
#pragma warning restore S3776 // Cognitive Complexity of methods should not be too high
    {
        for (int i = 0; i < currentTriggers.Count; i++)
        {
            var currentTrigger = currentTriggers[i];
            var newTrigger = newTriggers[i];
            if (currentTrigger.Key.Group != newTrigger.Key.Group) { return true; }
            if (currentTrigger.Key.Name != newTrigger.Key.Name) { return true; }
            if (currentTrigger.Description != newTrigger.Description) { return true; }
            if (currentTrigger.CalendarName != newTrigger.CalendarName) { return true; }
            if (currentTrigger.MisfireInstruction != newTrigger.MisfireInstruction) { return true; }
            if (currentTrigger.EndTimeUtc != newTrigger.EndTimeUtc) { return true; }
            if (currentTrigger.Priority != newTrigger.Priority) { return true; }
            if (currentTrigger.HasMillisecondPrecision != newTrigger.HasMillisecondPrecision) { return true; }
            //// ***IGNORE START TIME *** // if (currentTrigger.StartTimeUtc != newTrigger.StartTimeUtc) { return false; } *** IGNORE START TIME ***

            if (currentTrigger is ISimpleTrigger currentSimple && newTrigger is ISimpleTrigger newSimple)
            {
                if (currentSimple.RepeatCount != newSimple.RepeatCount) { return true; }
                if (currentSimple.RepeatInterval != newSimple.RepeatInterval) { return true; }
            }
            else if (currentTrigger is ICronTrigger currentCron && newTrigger is ICronTrigger newCron)
            {
#pragma warning disable S1066 // Mergeable "if" statements should be combined
                if (currentCron.CronExpressionString != newCron.CronExpressionString) { return true; }
#pragma warning restore S1066 // Mergeable "if" statements should be combined
            }

            var currentTriggerDataJson = GetDataMapJson(currentTrigger.JobDataMap);
            var newTriggerDataJson = GetDataMapJson(newTrigger.JobDataMap);
            if (currentTriggerDataJson != newTriggerDataJson) { return true; }
        }

        return false;
    }

    private static string GetDataMapJson(JobDataMap dataMap)
    {
        var dict = dataMap.WrappedMap;
        dict.Remove(Consts.JobId);
        dict.Remove(Consts.TriggerId);
        var json = JsonSerializer.Serialize(dict.ToImmutableSortedDictionary());
        return json;
    }
}