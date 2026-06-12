using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Model;
using System;
using System.Collections.Immutable;
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
        var currentTriggers = await scheduler.GetTriggersOfJob(jobKey);
        var dal = Resolve<IJobData>();
        (string? currentJobPropertiesYml, string? currentJobGlobalConfigKeysYml) = await dal.GetJobProperty(currentJobId);
        var currentJobDataJson = JsonSerializer.Serialize(currentJobDetails.JobDataMap.WrappedMap.ToImmutableSortedDictionary());

        var newJobDetails = BuildJobDetails(request, jobKey);
        var newJobPropertiesYml = GetJopPropertiesYml(request);
        var newJobGlobalConfigKeysYml = GetJobGlobalConfigKeysYml(request);
        AddAuthor(request, newJobDetails);
        AddCircuitBreaker(request, newJobDetails);
        AddLogRetentionDays(request, newJobDetails);
        BuildJobData(request, newJobDetails);
        var triggers = BuildTriggers(request, currentJobId);
        var newJobDataJson = JsonSerializer.Serialize(newJobDetails.JobDataMap.WrappedMap.ToImmutableSortedDictionary());

        // JobDetails
        if (currentJobDetails.ConcurrentExecutionDisallowed != newJobDetails.ConcurrentExecutionDisallowed) { return false; }
        if (currentJobDetails.Description != newJobDetails.Description) { return false; }
        if (currentJobDetails.Durable != newJobDetails.Durable) { return false; }
        if (currentJobDetails.JobType.FullName != newJobDetails.JobType.FullName) { return false; }
        if (currentJobDetails.RequestsRecovery != newJobDetails.RequestsRecovery) { return false; }
        if (currentJobDetails.Key.Group != newJobDetails.Key.Group) { return false; }
        if (currentJobDetails.Key.Name != newJobDetails.Key.Name) { return false; }

        // JobData
        if (currentJobDataJson != newJobDataJson) { return false; }

        // Properties
        if (currentJobPropertiesYml != newJobPropertiesYml) { return false; }

        // GlobalConfigKeys
        if (currentJobGlobalConfigKeysYml != newJobGlobalConfigKeysYml) { return false; }

        return true;
    }

    // *********************** ANY CHANGE TO THIS FUNCTION MUST BE REFLECTED IN JobDomainUpdatePartial.GetJobType FUNCTION ***********************
    private static void FillJobTypeAndConcurrent(Type type, SetJobDynamicRequest job)
    {
        const string concurrent = "Concurrent";
        const string noConcurrent = "NoConcurrent";
        var name = type.Name;
        name = name[name.IndexOf('.')..];
        if (name.EndsWith(concurrent))
        {
            job.JobType = name[..^(concurrent.Length)];
            job.Concurrent = true;
        }

        if (name.EndsWith(noConcurrent))
        {
            job.JobType = name[..^(noConcurrent.Length)];
            job.Concurrent = false;
        }
    }

    // *********************** ANY CHANGE TO THIS FUNCTION MUST BE REFLECTED IN JobDomainUpdatePartial.GetJobType FUNCTION ***********************
}