using Planar.API.Common.Entities;
using Planar.Common.Helpers;
using Planar.Service.Exceptions;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Quartz.Logging.OperationName;

namespace Planar.Service.API.Helpers;

public class JobKeyHelper(IScheduler scheduler)
{
    private readonly IScheduler _scheduler = scheduler;

    public static string? GetJobId(IJobDetail? job)
    {
        return JobHelper.GetJobId(job);
    }

    public static JobKey? GetJobKey(SetJobRequest metadata)
    {
        if (metadata.Name == null) { return null; }

        return string.IsNullOrEmpty(metadata.Group) ?
                        new JobKey(metadata.Name) :
                        new JobKey(metadata.Name, metadata.Group);
    }

    public async Task<string?> SafeGetJobId(JobKey jobKey)
    {
        var details = await _scheduler.GetJobDetail(jobKey);
        if (details == null) { return null; }
        return GetJobId(details);
    }

    public async Task<string?> GetJobId(JobKey jobKey)
    {
        var job = await ValidateJobExists(jobKey);
        return GetJobId(job);
    }

    public async Task<string?> GetJobId(string id)
    {
        var jobKey = await GetJobKey(id);
        if (jobKey == null) { return null; }

        var jobId = await GetJobId(jobKey);
        return jobId;
    }

    public async Task<JobKey> GetJobKey(string id)
    {
        return await GetJobKey(new JobOrTriggerKey { Id = id });
    }

    public async Task<JobKey> GetJobKey(JobOrTriggerKey key)
    {
        JobKey? result;
        if (key.Id.Contains('.'))
        {
            result = GetJobKeyByKey(key.Id);
        }
        else
        {
            result = await GetJobKeyById(key.Id);
            result ??= GetJobKeyByKey(key.Id);
        }

        await ValidateJobExists(result);
        if (result == null) { throw new RestNotFoundException($"job does not exist"); }
        return result;
    }

    public async Task<JobKey?> GetJobKeyById(string jobId)
    {
        JobKey? result = null;
        var keys = await _scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        foreach (var k in keys)
        {
            var jobDetails = await _scheduler.GetJobDetail(k);
            if (jobDetails != null)
            {
                var id = GetJobId(jobDetails);
                if (id == jobId)
                {
                    result = k;
                    break;
                }
            }
        }

        return result;
    }

    public async Task<bool> IsJobGroupExists(string group)
    {
        var all = await _scheduler.GetJobGroupNames();
        return all.Contains(group);
    }

    public async Task<IJobDetail> ValidateJobExists(JobKey? jobKey)
    {
        if (jobKey == null) { throw new RestNotFoundException($"job does not exist"); }
        var exists = await _scheduler.GetJobDetail(jobKey);
        return exists ?? throw new RestNotFoundException($"job with key '{KeyHelper.GetKeyTitle(jobKey)}' does not exist");
    }

    public static bool IsSystemJobKey(JobKey jobKey)
    {
        return IsSystemJobGroup(jobKey.Group);
    }

    public static bool IsSystemJobGroup(string group)
    {
        return string.Equals(group, Consts.PlanarSystemGroup, StringComparison.OrdinalIgnoreCase);
    }

    private static JobKey? GetJobKeyByKey(string key)
    {
        JobKey? result = null;
        if (key != null)
        {
            var index = key.IndexOf('.');
            if (index == -1)
            {
                result = new JobKey(key);
            }
            else
            {
                result = new JobKey(key[(index + 1)..], key[0..index]);
            }
        }

        return result;
    }
}