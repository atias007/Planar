using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Model;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.API;

public partial class JobDomain
{
    private static void FillEstimatedEndTime(List<RunningJobDetails> runningJobs)
    {
        foreach (var item in runningJobs)
        {
            FillEstimatedEndTime(item);
        }
    }

    private static void FillEstimatedEndTime(RunningJobDetails runningJob)
    {
        if (runningJob.Progress < 1) { return; }
        var factor = 100 - runningJob.Progress;
        var ms = (runningJob.RunTime.TotalMilliseconds / runningJob.Progress) * factor;
        runningJob.EstimatedEndTime = TimeSpan.FromMilliseconds(ms);
    }

    private static SetJobDynamicRequest? GetJobDynamicRequestFromFilename(string filename)
    {
        try
        {
            var yml = File.ReadAllText(filename);
            var request = YmlUtil.Deserialize<SetJobDynamicRequest>(yml);
            return request;
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"fail to read yml file: {filename}", ex);
        }
    }

    private static void ValidateDataKeyExists(IJobDetail details, string key, string jobId)
    {
        if (details == null || !details.JobDataMap.ContainsKey(key))
        {
            throw new RestValidationException($"{key}", $"data with Key '{key}' could not found in job '{jobId}' (name '{details?.Key.Name}' and group '{details?.Key.Group}')");
        }
    }

    private async Task DeleteJobStatistics(string jobId)
    {
        var dal = Resolve<IMetricsData>();
        var s1 = new JobDurationStatistic { JobId = jobId };
        await dal.DeleteJobStatistic(s1);
        var s2 = new JobEffectedRowsStatistic { JobId = jobId };
        await dal.DeleteJobStatistic(s2);
    }

    private async Task DeleteMonitorOfJob(JobKey jobKey)
    {
        var dal = Resolve<IMonitorData>();
        await dal.DeleteMonitorByJobId(jobKey.Group, jobKey.Name);
        if (!await JobGroupExists(jobKey.Group))
        {
            await dal.DeleteMonitorByJobGroup(jobKey.Group);
        }
    }

    private async Task DeleteJobHistory(string jobId)
    {
        var dal = Resolve<IHistoryData>();
        await dal.ClearJobHistory(jobId);
    }

    private async Task<AvailableJob?> GetAvailableJob(string filename, string jobsFolder, bool update)
    {
        try
        {
            var request = GetJobDynamicRequestFromFilename(filename);
            if (request == null) { return null; }
            if (string.IsNullOrWhiteSpace(request.JobType)) { return null; }
            if (string.IsNullOrWhiteSpace(request.Name)) { return null; }
            if (!ServiceUtil.JobTypes.Any(j => string.Equals(j, request.JobType, StringComparison.OrdinalIgnoreCase))) { return null; }

            var key = JobKeyHelper.GetJobKey(request);
            if (key == null) { return null; }

            var details = await Scheduler.GetJobDetail(key);
            if (details == null && update) { return null; }
            if (details != null && !update) { return null; }

            var fileInfo = new FileInfo(filename);
            var fullFolder = fileInfo.Directory;
            if (fullFolder == null) { return null; }
            var relativeFolder =
                fullFolder.FullName.Length == jobsFolder.Length ?
                string.Empty :
                fullFolder.FullName[(jobsFolder.Length + 1)..];
            var result = new AvailableJob
            {
                Name = key.ToString(),
                JobFile = Path.Combine(relativeFolder, fileInfo.Name)
            };

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Fail to get avaliable job folder info");
        }

        return null;
    }

    private async Task<IReadOnlyCollection<JobKey>> GetJobKeys(GetAllJobsRequest request)
    {
        var matcher =
            string.IsNullOrEmpty(request.Group) ?
            GroupMatcher<JobKey>.AnyGroup() :
            GroupMatcher<JobKey>.GroupEquals(request.Group);

        switch (request.JobCategory)
        {
            case AllJobsMembers.AllUserJobs:
                var result = await Scheduler.GetJobKeys(matcher);
                var list = result.Where(x => x.Group != Consts.PlanarSystemGroup).ToList();
                return new ReadOnlyCollection<JobKey>(list);

            case AllJobsMembers.AllSystemJobs:
                return await Scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(Consts.PlanarSystemGroup));

            default:
            case AllJobsMembers.All:
                return await Scheduler.GetJobKeys(matcher);
        }
    }

    private async Task<TriggerRowDetails> GetTriggersDetails(JobKey jobKey)
    {
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

    private async Task<JobActiveMembers> GetJobActiveMode(JobKey jobKey)
    {
        return await JobHelper.GetJobActiveMode(Scheduler, jobKey);
    }

    private async Task<bool> JobGroupExists(string jobGroup)
    {
        var allGroups = await Scheduler.GetJobGroupNames();
        return allGroups.Contains(jobGroup);
    }

    private async Task<JobBasicDetails> MapJobDetailsSlim(IJobDetail source)
    {
        var target = new JobBasicDetails();
        SchedulerUtil.MapJobRowDetails(source, target);
        target.Active = await GetJobActiveMode(source.Key);
        target.AutoResume = await AutoResumeJobUtil.GetAutoResumeDate(Scheduler, source.Key);
        return target;
    }

    private async Task<JobDetails> MapJobDetails(IJobDetail source, JobDataMap? dataMap = null)
    {
        var target = new JobDetails();
        SchedulerUtil.MapJobRowDetails(source, target);
        target.Active = await GetJobActiveMode(source.Key);
        target.AutoResume = await AutoResumeJobUtil.GetAutoResumeDate(Scheduler, source.Key);

        dataMap ??= source.JobDataMap;
        target.Concurrent = !source.ConcurrentExecutionDisallowed;
        target.Author = JobHelper.GetJobAuthor(source);
        target.LogRetentionDays = JobHelper.GetLogRetentionDays(source);
        target.Durable = source.Durable;
        target.RequestsRecovery = source.RequestsRecovery;
        target.DataMap = Global.ConvertDataMapToDictionary(dataMap);
        target.Properties = await DataLayer.GetJobProperty(target.Id) ?? string.Empty;

        var cbm = JobHelper.GetJobCircuitBreaker(source);
        if (cbm == null) { return target; }

        target.CircuitBreaker = Mapper.Map<JobCircuitBreaker>(cbm);
        var trigger = await SchedulerUtil.GetCircuitBreakerTrigger(source.Key);
        if (trigger != null)
        {
            target.CircuitBreaker.WillBeResetAt = trigger.GetNextFireTimeUtc()?.LocalDateTime;
            if (
                trigger.JobDataMap.TryGetString(AutoResumeJobUtil.Created, out var createdText) &&
                DateTime.TryParse(createdText, CultureInfo.CurrentCulture, out var created))
            {
                target.CircuitBreaker.ActivatedAt = created;
            }
        }

        return target;
    }

    private async Task<bool> CancelQueuedResumeJob(JobKey jobKey)
    {
        var cancelAutoResume = await AutoResumeJobUtil.CancelQueuedResumeJob(Scheduler, jobKey);
        if (cancelAutoResume) { AuditJobSafe(jobKey, "cancel existing auto resume"); }
        return cancelAutoResume;
    }
}