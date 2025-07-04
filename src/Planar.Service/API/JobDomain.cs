﻿using CloudNative.CloudEvents;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Common.Helpers;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Planar.Service.Model;
using Planar.Service.Monitor;
using Planar.Service.Reports;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Planar.Service.API;

public partial class JobDomain(IServiceProvider serviceProvider, IServiceScopeFactory scopeFactory)
    : BaseJobBL<JobDomain, IJobData>(serviceProvider), IJobActions
{
    private static TimeSpan _longPullingSpan = TimeSpan.FromMinutes(5);

    #region Data

    public async Task ClearData(string id)
    {
        var info = await GetJobDetailsForDataCommands(id);
        if (info.JobDetails == null) { return; }

        var validKeys = info.JobDetails.JobDataMap.Keys.Where(Consts.IsDataKeyValid);
        var keyCount = validKeys.Count();
        foreach (var key in validKeys)
        {
            info.JobDetails.JobDataMap.Remove(key);
        }

        var triggers = await Scheduler.GetTriggersOfJob(info.JobKey);

        // Reschedule job
        MonitorUtil.Lock(info.JobKey, lockSeconds: 3, MonitorEvents.JobAdded, MonitorEvents.JobPaused);
        await Scheduler.ScheduleJob(info.JobDetails, triggers, true);
        await Scheduler.PauseJob(info.JobKey);

        AuditJobSafe(info.JobKey, $"clear job data. {keyCount} key(s)");
    }

    public async Task PutData(JobOrTriggerDataRequest request, PutMode mode)
    {
        var info = await GetJobDetailsForDataCommands(request.Id, request.DataKey);
        ValidateMaxLength(request.DataValue, 1000, "value", string.Empty);
        if (info.JobDetails == null) { return; }

        if (info.JobDetails.JobDataMap.ContainsKey(request.DataKey))
        {
            if (mode == PutMode.Add)
            {
                throw new RestConflictException($"data with key '{request.DataKey}' already exists");
            }

            info.JobDetails.JobDataMap.Put(request.DataKey, request.DataValue);
            AuditJobSafe(info.JobKey, $"update job data with key '{request.DataKey}'", new { value = request.DataValue?.Trim() });
        }
        else
        {
            if (mode == PutMode.Update)
            {
                throw new RestNotFoundException($"data with key '{request.DataKey}' not found");
            }

            var dataCount = CountUserJobDataItems(info.JobDetails.JobDataMap);
            if (dataCount >= Consts.MaximumJobDataItems)
            {
                throw new RestValidationException("job data", $"job data items exceeded maximum limit of {Consts.MaximumJobDataItems}");
            }

            info.JobDetails.JobDataMap.Put(request.DataKey, request.DataValue);
            AuditJobSafe(info.JobKey, $"add job data with key '{request.DataKey}'", new { value = request.DataValue?.Trim() });
        }

        var triggers = await Scheduler.GetTriggersOfJob(info.JobKey);

        // Reschedule job
        MonitorUtil.Lock(info.JobKey, lockSeconds: 3, MonitorEvents.JobAdded, MonitorEvents.JobPaused);
        await Scheduler.ScheduleJob(info.JobDetails, triggers, true);

        // Pause job
        await Scheduler.PauseJob(info.JobKey);
    }

    public async Task RemoveData(string id, string key)
    {
        var info = await GetJobDetailsForDataCommands(id, key);
        if (info.JobDetails == null) { return; }

        ValidateDataKeyExists(info.JobDetails, key, id);
        var auditValue = PlanarConvert.ToString(info.JobDetails.JobDataMap[key]);
        info.JobDetails.JobDataMap.Remove(key);
        var triggers = await Scheduler.GetTriggersOfJob(info.JobKey);

        // Reschedule job
        MonitorUtil.Lock(info.JobKey, lockSeconds: 3, MonitorEvents.JobAdded, MonitorEvents.JobPaused);
        await Scheduler.ScheduleJob(info.JobDetails, triggers, true);
        await Scheduler.PauseJob(info.JobKey);

        AuditJobSafe(info.JobKey, $"remove job data with key '{key}'", new { value = auditValue?.Trim() });
    }

    private async Task<DataCommandDto> GetJobDetailsForDataCommands(string jobId, string? key = null)
    {
        // Get Job
        var jobKey = await JobKeyHelper.GetJobKey(jobId);
        var result = new DataCommandDto
        {
            JobKey = jobKey,
            JobDetails = await JobKeyHelper.ValidateJobExists(jobKey)
        };

        ValidateSystemJob(jobKey);
        if (key != null)
        {
            ValidateSystemDataKey(key);
        }
        await ValidateJobPaused(jobKey);
        await ValidateJobNotRunning(jobKey);
        return result;
    }

    #endregion Data

    public enum PutMode
    {
        Add,
        Update
    }

    public void FailOverPublish(CloudEvent request)
    {
        var context = ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var userAgent = context.HttpContext?.Request.Headers.UserAgent.ToString();
        var failoverAgent = $"{nameof(Planar)}.{nameof(Job)}.FailOverProxy";
        if (string.Equals(failoverAgent, userAgent, StringComparison.OrdinalIgnoreCase))
        {
            MqttBrokerService.OnInterceptingPublishAsync(request);
            return;
        }

        throw new RestForbiddenException();
    }

    public static string GetJobFileTemplate(string typeName)
    {
        var notFoundException = new Lazy<RestNotFoundException>(() => new RestNotFoundException($"type '{typeName}' could not be found"));

        Assembly assembly;

        try
        {
            assembly = Assembly.Load(typeName);
        }
        catch
        {
            throw notFoundException.Value;
        }

        var resources = assembly.GetManifestResourceNames();
        var resourceName =
            Array.Find(resources, r => r.Equals($"{typeName}.JobFile.yml", StringComparison.CurrentCultureIgnoreCase)) ??
            throw notFoundException.Value;

        using Stream? stream =
            assembly.GetManifestResourceStream(resourceName) ??
            throw new RestNotFoundException("jobfile.yml resource could not be found");

        using StreamReader reader = new(stream);
        var result = reader.ReadToEnd();

        if (string.IsNullOrEmpty(result))
        {
            throw new RestNotFoundException("jobfile.yml resource could not be found");
        }

        return result;
    }

    public static IEnumerable<string> GetJobTypes()
    {
        return ServiceUtil.JobTypes;
    }

    public async Task<bool> Cancel(FireInstanceIdRequest request)
    {
        var stop = await SchedulerUtil.StopRunningJob(request.FireInstanceId);
        if (AppSettings.Cluster.Clustering && !stop)
        {
            stop = await ClusterUtil.StopRunningJob(request.FireInstanceId);
        }

        if (!stop && !await SchedulerUtil.IsRunningInstanceExistOnLocal(request.FireInstanceId))
        {
            throw new RestNotFoundException($"instance id '{request.FireInstanceId}' is not running");
        }

        return stop;
    }

    public async Task<JobDetails> Get(string id)
    {
        var jobKey = await JobKeyHelper.GetJobKey(id);
        var info =
            await Scheduler.GetJobDetail(jobKey) ??
            throw new RestNotFoundException($"job with key '{KeyHelper.GetKeyTitle(jobKey)}' does not exist");

        var result = await MapJobDetails(info);

        var triggers = await GetTriggersDetails(jobKey);
        result.SimpleTriggers = triggers.SimpleTriggers;
        result.CronTriggers = triggers.CronTriggers;

        return result;
    }

    public async Task<IEnumerable<string>> GetAllIds()
    {
        var request = new GetAllJobsRequest { JobCategory = AllJobsMembers.All };
        var jobKeys = await GetJobKeys(request);
        var ids = jobKeys.Select(async k => await JobKeyHelper.GetJobId(k));
        await Task.WhenAll(ids);
        var jobIds = ids.Select(t => t.Result).Where(t => t != null);
        var result = jobIds
            .Select(i => i ?? string.Empty)
            .Where(i => !string.IsNullOrWhiteSpace(i));
        return result;
    }

    public async Task<PagingResponse<JobBasicDetails>> GetAll(GetAllJobsRequest request)
    {
        var jobs = new List<IJobDetail>();

        // get all jobs
        foreach (var jobKey in await GetJobKeys(request))
        {
            var info = await Scheduler.GetJobDetail(jobKey);
            if (info == null) { continue; }
            jobs.Add(info);
        }

        // filter by job type
        if (!string.IsNullOrEmpty(request.JobType))
        {
            jobs = jobs
                .Where(r => string.Equals(SchedulerUtil.GetJobTypeName(r.JobType), request.JobType, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // filter by search
        if (!string.IsNullOrWhiteSpace(request.Filter))
        {
            jobs = jobs
                .Where(r =>
                    r.Key.Name.Contains(request.Filter, StringComparison.OrdinalIgnoreCase) ||
                    r.Key.Group.Contains(request.Filter, StringComparison.OrdinalIgnoreCase) ||
                    (r.Description != null && r.Description.Contains(request.Filter, StringComparison.OrdinalIgnoreCase))
                    )
                .ToList();
        }

        // fill IsActive property
        var jobList = jobs
            .Select(async j => await MapJobDetailsSlim(j))
            .Select(t => t.Result);

        // filter by active
        if (request.Active.HasValue)
        {
            if (request.Active.Value)
            {
                jobList = jobList.Where(r => r.Active != JobActiveMembers.Inactive && r.Active != JobActiveMembers.NoTrigger);
            }
            else
            {
                jobList = jobList.Where(r => r.Active == JobActiveMembers.Inactive || r.Active == JobActiveMembers.NoTrigger);
            }
        }

        // paging & order by
        var result = jobList
            .Select(j => j)
            .OrderBy(j => j.Group)
            .ThenBy(j => j.Name)
            .SetPaging(request)
            .ToList();

        return new PagingResponse<JobBasicDetails>(request, result, jobList.Count());
    }

    public async Task<PagingResponse<JobAuditDto>> GetAudits(PagingRequest request)
    {
        var query = DataLayer.GetAudits();
        var result = await query.ProjectToWithPagingAsyc<JobAudit, JobAuditDto>(Mapper, request);
        return result;
    }

    public async Task<IEnumerable<JobAuditDto>> GetAuditsForReport(DateScope dateScope)
    {
        var query = DataLayer.GetAuditsForReport(dateScope);
        var result = await Mapper.ProjectTo<JobAuditDto>(query).ToListAsync();
        return result;
    }

    public async Task<IEnumerable<AvailableJob>> GetAvailableJobs(bool update)
    {
        var result = new List<AvailableJob>();
        var folder = ServiceUtil.GetJobsFolder();
        var files = Directory.GetFiles(folder, FolderConsts.JobFileExtPattern, SearchOption.AllDirectories);
        foreach (var f in files)
        {
            var job = await GetAvailableJob(f, folder, update);
            if (job != null) { result.Add(job); }
        }

        return result.OrderBy(a => a.Name);
    }

    public async Task<JobDescription> GetDescription(string id)
    {
        var monitorDomain = ServiceProvider.GetRequiredService<MonitorDomain>();
        var historyDomain = ServiceProvider.GetRequiredService<HistoryDomain>();
        var statisticsDomain = ServiceProvider.GetRequiredService<MetricsDomain>();

        var historyRequest = new GetHistoryRequest { JobId = id, PageSize = 10 };
        var details = await Get(id);
        var monitorsTask = monitorDomain.GetByJob(id);
        var audit = await GetJobAudits(id, new PagingRequest(1, 10));
        var historyTask = historyDomain.GetHistory(historyRequest);
        var statisticsTask = statisticsDomain.GetJobMetrics(id);
        var result = new JobDescription
        {
            Details = details,
            Audits = audit,
            History = await historyTask,
            Monitors = new PagingResponse<MonitorItem>(await monitorsTask),
            Metrics = await statisticsTask
        };

        return result;
    }

    public async Task<JobAuditDto> GetJobAudit(int id)
    {
        var query = DataLayer.GetJobAudit(id);
        var entity = await Mapper.ProjectTo<JobAuditWithInfoDto>(query).FirstOrDefaultAsync();
        var result = ValidateExistingEntity(entity, "job audit");
        return result;
    }

    public async Task<PagingResponse<JobAuditDto>> GetJobAudits(string id, PagingRequest paging)
    {
        var jobKey = await JobKeyHelper.GetJobKey(id);
        var jobId = await JobKeyHelper.GetJobId(jobKey) ?? string.Empty;
        var firstId = await DataLayer.GetJobFirstAudit(jobId) ?? 0;
        var query = DataLayer.GetJobAudits(jobId, firstId);
        var result = await query.ProjectToWithPagingAsyc<JobAudit, JobAuditDto>(Mapper, paging);
        return result;
    }

    private sealed class JobFileValidationRecord
    {
        [YamlMember(Alias = "job type")]
        public string? JobType { get; set; }

        public string? Name { get; set; } = null!;
    }

    public async Task<string> GetJobFilename(string id)
    {
        var key = await JobKeyHelper.GetJobKey(id);
        var jobId = await JobKeyHelper.GetJobId(key);
        if (string.IsNullOrWhiteSpace(jobId)) { throw NotFound(id); }
        var properties = await DataLayer.GetJobProperty(jobId);
        if (string.IsNullOrWhiteSpace(properties))
        {
            throw NotFound(id);
        }

        var propDic = YmlUtil.Deserialize<dynamic>(properties) as Dictionary<object, object> ?? [];
        if (!propDic.TryGetValue("path", out var pathObj)) { throw NotFound(id); }
        var path = Convert.ToString(pathObj);
        if (string.IsNullOrWhiteSpace(path)) { throw NotFound(id); }
        var fullpath = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, path);

        var jobsFolder = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs);

        var files = Directory.EnumerateFiles(fullpath, "*.yml", SearchOption.TopDirectoryOnly);
        var validFiles = files.Where(f =>
        {
            try
            {
                var yml = File.ReadAllText(f);
                var record = YmlUtil.Deserialize<JobFileValidationRecord>(yml);
                return !string.IsNullOrWhiteSpace(record.JobType) && !string.IsNullOrWhiteSpace(record.Name);
            }
            catch
            {
                return false;
            }
        })
        .ToList();

        var count = validFiles.Count;
        if (count == 0) { throw NotFound(id, path); }
        if (count > 1) { throw new RestValidationException("id", $"more than one ({count}) valid yml jobfile found in '{path}' folder"); }

        var jobfile = Path.GetRelativePath(jobsFolder, validFiles[0]);
        return jobfile;

        static Exception NotFound(string id, string? path = null)
        {
            var message = string.IsNullOrWhiteSpace(path) ?
                $"no valid yml jobfile found for '{id}' job" :
                $"no valid yml jobfile found for '{id}' job in '{path}' folder";

            return new RestNotFoundException(message);
        }
    }

    public async Task<IEnumerable<string>> GetJobGroupNames()
    {
        var result = (await Scheduler.GetJobGroupNames())
            .Where(g => !string.Equals(g, Consts.PlanarSystemGroup, StringComparison.OrdinalIgnoreCase));
        return result;
    }

    public async Task<LastInstanceId?> GetLastInstanceId(string id, DateTime invokeDate, CancellationToken cancellationToken)
    {
        var jobKey = await JobKeyHelper.GetJobKey(id);

        if (JobKeyHelper.IsSystemJobKey(jobKey))
        {
            throw new RestValidationException("id", "this is system job and it does not have instance id");
        }

        var dal = Resolve<IHistoryData>();

        for (int i = 0; i < 60; i++)
        {
            var result = await dal.GetLastInstanceId(jobKey, invokeDate, cancellationToken);
            if (result != null) { return result; }
            if (i % 10 == 0)
            {
                var running = await GetRunning();
                var exists = running.Exists(d => d.Id == id || string.Equals($"{d.Group}.{d.Name}", id, StringComparison.OrdinalIgnoreCase));
                if (exists)
                {
                    throw new RestConflictException();
                }
            }

            await Task.Delay(500, cancellationToken);
        }

        return null;
    }

    public async Task<DateTime?> GetNextRunning(string id)
    {
        var jobKey = await JobKeyHelper.GetJobKey(id);
        var triggers = await Scheduler.GetTriggersOfJob(jobKey);
        DateTime? result = null;
        foreach (var t in triggers)
        {
            var state = await Scheduler.GetTriggerState(t.Key);
            if (state == TriggerState.Paused) { continue; }
            var next = t.GetNextFireTimeUtc();
            if (next == null) { continue; }
            var nextDate = next.Value.LocalDateTime;
            if (result == null || nextDate < result)
            {
                result = nextDate;
            }
        }

        return result;
    }

    public async Task<DateTime?> GetPreviousRunning(string id)
    {
        var jobKey = await JobKeyHelper.GetJobKey(id);
        var triggers = await Scheduler.GetTriggersOfJob(jobKey);
        DateTime? result = null;
        foreach (var t in triggers)
        {
            var prev = t.GetPreviousFireTimeUtc();
            if (prev == null) { continue; }
            var prevDate = prev.Value.LocalDateTime;
            if (result == null || prevDate > result)
            {
                result = prevDate;
            }
        }

        return result;
    }

    public async Task<List<RunningJobDetails>> GetRunning()
    {
        var result = await SchedulerUtil.GetRunningJobs();
        if (AppSettings.Cluster.Clustering)
        {
            var clusterResult = await ClusterUtil.GetRunningJobs();
            result ??= [];

            if (clusterResult != null)
            {
                result.AddRange(clusterResult);
            }
        }

        result = result.Where(r => r.Group != Consts.PlanarSystemGroup).ToList();

        FillEstimatedEndTime(result);

        return result;
    }

    public async Task<RunningJobDetails> GetRunning(string instanceId)
    {
        var result = await SchedulerUtil.GetRunningJob(instanceId);
        if (result == null && AppSettings.Cluster.Clustering)
        {
            result = await ClusterUtil.GetRunningJob(instanceId);
        }

        if (result == null)
        {
            throw new RestNotFoundException();
        }

        FillEstimatedEndTime(result);

        return result;
    }

    public async Task<RunningJobData> GetRunningData(string instanceId)
    {
        var result = await SchedulerUtil.GetRunningData(instanceId);
        if (result != null)
        {
            return result;
        }

        if (AppSettings.Cluster.Clustering)
        {
            result = await ClusterUtil.GetRunningData(instanceId);
        }

        if (result == null)
        {
            throw new RestNotFoundException($"instanceId {instanceId} was not found");
        }

        return result;
    }

    public async Task<RunningJobDetails> GetRunningInstanceLongPolling(
       string instanceId,
       int? progress,
       int? effectedRows,
       int? exceptionsCount,
       CancellationToken cancellationToken)
    {
        var access = ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var hash = Convert.ToString(access.HttpContext?.Request.Query["hash"]);
        if (string.IsNullOrWhiteSpace(hash))
        {
            return await GetRunningInstanceLongPollingV2(instanceId, progress, effectedRows, exceptionsCount, cancellationToken);
        }
        else
        {
            return await GetRunningInstanceLongPollingV1(instanceId, hash, cancellationToken);
        }
    }

    public async Task<RunningJobDetails> GetRunningInstanceLongPollingV1(
        string instanceId,
        string hash,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_longPullingSpan);
        while (!cts.IsCancellationRequested)
        {
            var data = await GetRunning(instanceId);
            var currentHash = $"{data.Progress}.{data.EffectedRows}.{data.ExceptionsCount}";
            if (currentHash != hash)
            {
                return data;
            }

            try
            {
                await Task.Delay(500, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return data;
            }
        }

        throw new RestNotFoundException();
    }

    public async Task<RunningJobDetails> GetRunningInstanceLongPollingV2(
       string instanceId,
       int? progress,
       int? effectedRows,
       int? exceptionsCount,
       CancellationToken cancellationToken)
    {
        var hash = $"{progress}.{effectedRows}.{exceptionsCount}";
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_longPullingSpan);
        while (!cts.IsCancellationRequested)
        {
            var data = await GetRunning(instanceId);
            if (progress == null && effectedRows == null && exceptionsCount == null) { return data; }

            var currentProgress = progress == null ? (int?)null : data.Progress;
            var currentEffectedRows = effectedRows == null ? null : data.EffectedRows;
            var currentExceptionsCount = exceptionsCount == null ? (int?)null : data.ExceptionsCount;

            var currentHash = $"{currentProgress}.{currentEffectedRows}.{currentExceptionsCount}";
            if (currentHash != hash)
            {
                return data;
            }

            try
            {
                await Task.Delay(500, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return data;
            }
        }

        throw new RestRequestTimeoutException();
    }

    public async Task<IEnumerable<KeyValueItem>> GetSettings(string id)
    {
        var result = new List<KeyValueItem>();
        var jobId = await JobKeyHelper.GetJobId(id) ?? string.Empty;
        var properties = await DataLayer.GetJobProperty(jobId);

        if (string.IsNullOrEmpty(properties))
        {
            return result;
        }

        string jobPath;
        try
        {
            var pathObj = YmlUtil.Deserialize<JobPropertiesWithPath>(properties);
            jobPath = pathObj.Path ?? string.Empty;
        }
        catch (Exception)
        {
            return result;
        }

        var settings = JobSettingsLoader.LoadJobSettings(jobPath, Global.GlobalConfig);
        result = settings.Select(d => new KeyValueItem(d.Key, d.Value)).ToList();

        return result;
    }

    public async Task Invoke(InvokeJobRequest request)
    {
        var jobKey = await JobKeyHelper.GetJobKey(request);
        ValidateDataMap(request.Data, "invoke");

        request.Data ??= [];
        if (request.NowOverrideValue.HasValue)
        {
            request.Data.Add(Consts.NowOverrideValue, request.NowOverrideValue.Value.ToString());
        }

        if (request.Timeout.HasValue)
        {
            var timeoutValue = request.Timeout.Value.Ticks.ToString();
            request.Data.Add(Consts.TriggerTimeout, timeoutValue);
        }

        if (request.Data.Count != 0)
        {
            var data = new JobDataMap(request.Data);
            await Scheduler.TriggerJob(jobKey, data);
        }
        else
        {
            await Scheduler.TriggerJob(jobKey);
        }

        AuditJobSafe(jobKey, "job manually invoked", request);
    }

    public async Task Pause(PauseResumeJobRequest request)
    {
        var jobKey = await JobKeyHelper.GetJobKey(request);
        ValidateSystemJob(jobKey);

        await CancelQueuedResumeJob(jobKey);
        await Scheduler.PauseJob(jobKey);

        if (request.AutoResumeDate == null)
        {
            Audit(false, null);
            return;
        }

        // Handle auto resume
        var job = await Scheduler.GetJobDetail(jobKey);
        if (job == null)
        {
            Audit(false, null);
            return;
        }

        await AutoResumeJobUtil.QueueResumeJob(Scheduler, jobKey, request.AutoResumeDate.Value, AutoResumeTypes.AutoResume);
        Audit(true, request.AutoResumeDate.Value);

        // ----------------------- Audit Function ----------------------- //
        void Audit(bool scheduleAutoResume, DateTime? autoResumeDate)
        {
            AuditJobSafe(jobKey, "job paused");
            if (scheduleAutoResume && autoResumeDate != null)
            {
                var info = new Dictionary<string, string>
                {
                    { "auto resume date", autoResumeDate.Value.ToShortDateString() },
                    { "auto resume time",  autoResumeDate.Value.ToString("HH:mm:ss")}
                };

                AuditJobSafe(jobKey, "schedule auto resume", info);
            }
        }
    }

    public async Task PauseGroup(PauseResumeGroupRequest request)
    {
        ValidateSystemGroup(request.Name);
        var keys = await Scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(request.Name));
        if (keys.Count == 0)
        {
            throw new RestNotFoundException($"group '{request.Name}' was not found");
        }
        await Scheduler.PauseJobs(GroupMatcher<JobKey>.GroupEquals(request.Name));
        foreach (var key in keys)
        {
            try
            {
                AuditJobSafe(key, $"job paused while pause job group '{request.Name}'");
                await CancelQueuedResumeJob(key);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "fail to audit/cancel auto resume for job '{Key}', while pause job group '{Name}'", key, request.Name);
            }
        }
    }

    public async Task<JobKey> InternalJobPrepareQueueInvoke(QueueInvokeJobRequest request)
    {
        var jobKey = await JobKeyHelper.GetJobKey(request);
        ValidateSystemJob(jobKey);
        ValidateDataMap(request.Data, "queue invoke");
        return jobKey;
    }

    public async Task<PlanarIdResponse> InternalJobQueueInvoke(QueueInvokeJobRequest request, JobKey jobKey)
    {
        var job = await Scheduler.GetJobDetail(jobKey);
        if (job == null) { return new PlanarIdResponse(); }

        // build new trigger
        var triggerId = ServiceUtil.GenerateId();
        var triggerKey = new TriggerKey($"DueTo.{request.DueDate:yyyyMMdd.HHmmss}", Consts.QueueInvokeTriggerGroup);
        var exists = await Scheduler.GetTrigger(triggerKey);
        if (exists != null)
        {
            throw new RestValidationException("due date", $"job already has queue invoke trigger with date {request.DueDate:yyyy-MM-dd HH:mm:ss}");
        }

        var newTrigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .UsingJobData(Consts.TriggerId, triggerId)
            .WithPriority(int.MaxValue - 2)
            .StartAt(request.DueDate)
            .WithSimpleSchedule(b =>
            {
                b.WithRepeatCount(0).WithMisfireHandlingInstructionFireNow();
            })
            .ForJob(job);

        if (request.Timeout.HasValue)
        {
            var timeoutValue = request.Timeout.Value.Ticks.ToString();
            newTrigger = newTrigger.UsingJobData(Consts.TriggerTimeout, timeoutValue);
        }

        request.Data ??= [];
        if (request.NowOverrideValue.HasValue)
        {
            request.Data.Add(Consts.NowOverrideValue, request.NowOverrideValue.Value.ToString());
        }

        foreach (var item in request.Data)
        {
            newTrigger = newTrigger.UsingJobData(item.Key, item.Value ?? string.Empty);
        }

        try
        {
            // Schedule Job
            await Scheduler.ScheduleJob(newTrigger.Build());
        }
        catch (Exception ex)
        {
            ValidateTriggerNeverFire(ex);
            throw;
        }

        return new PlanarIdResponse { Id = triggerId };
    }

    public async Task<PlanarIdResponse> QueueInvoke(QueueInvokeJobRequest request)
    {
        var jobKey = await InternalJobPrepareQueueInvoke(request);
        var response = await InternalJobQueueInvoke(request, jobKey);
        AuditJobSafe(jobKey, "job queue invoked", request);
        return response;
    }

    public async Task Remove(string id)
    {
        var jobKey = await JobKeyHelper.GetJobKey(id);
        var jobId = await JobKeyHelper.GetJobId(jobKey) ?? string.Empty;
        ValidateSystemJob(jobKey);
        await ValidateSequenceStepJob(jobKey);

        await Scheduler.DeleteJob(jobKey);
        AuditJobSafe(jobKey, "job deleted");
        _ = ClearJobInfo(jobId, jobKey, id);
    }

    private async Task ClearJobInfo(string jobId, JobKey jobKey, string id)
    {
        // Delete property
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dataLayer = scope.ServiceProvider.GetRequiredService<IJobData>();
            await dataLayer.DeleteJobProperty(jobId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "fail to delete properties after delete job id {Id}", id);
        }

        // Delete job audit
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dataLayer = scope.ServiceProvider.GetRequiredService<IJobData>();
            await dataLayer.DeleteJobAudit(jobId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "fail to delete audit after delete job id {Id}", id);
        }

        // Delete monitor
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var monitordal = scope.ServiceProvider.GetRequiredService<IMonitorData>();
            await DeleteMonitorOfJob(monitordal, jobKey);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "fail to delete monitor after delete job id {Id}", id);
        }

        // Delete metrics
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var merticsdal = scope.ServiceProvider.GetRequiredService<IMetricsData>();
            await DeleteJobStatistics(merticsdal, jobId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "fail to delete job metrics after delete job id {Id}", id);
        }

        // Delete history
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var historydal = scope.ServiceProvider.GetRequiredService<IHistoryData>();
            await historydal.ClearJobHistory(jobId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "fail to delete job history after delete job id {Id}", id);
        }
    }

    public async Task Resume(PauseResumeJobRequest request)
    {
        var jobKey = await JobKeyHelper.GetJobKey(request);
        ValidateSystemJob(jobKey);

        await CancelQueuedResumeJob(jobKey);

        if (request.AutoResumeDate == null)
        {
            await Scheduler.ResumeJob(jobKey);
            AuditJobSafe(jobKey, "job resumed");
            await CancelQueuedResumeJob(jobKey);
        }
        else
        {
            await AutoResumeJobUtil.QueueResumeJob(Scheduler, jobKey, request.AutoResumeDate.Value, AutoResumeTypes.AutoResume);
            AuditJobSafe(jobKey, "schedule auto resume", new { autoResumeDate = request.AutoResumeDate.Value });
        }
    }

    public async Task ResumeGroup(PauseResumeGroupRequest request)
    {
        ValidateSystemGroup(request.Name);
        var keys = await Scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(request.Name));
        if (keys.Count == 0)
        {
            throw new RestNotFoundException($"group '{request.Name}' was not found");
        }

        await Scheduler.ResumeJobs(GroupMatcher<JobKey>.GroupEquals(request.Name));

        foreach (var key in keys)
        {
            try
            {
                AuditJobSafe(key, $"job resume while resume job group '{request.Name}'");
                await CancelQueuedResumeJob(key);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "fail to audit/cancel auto resume for job '{Key}', while resume job group '{Name}'", key, request.Name);
            }
        }
    }

    public async Task SetAuthor(SetJobAuthorRequest request)
    {
        var jobKey = await JobKeyHelper.GetJobKey(request);
        ValidateSystemJob(jobKey);
        await ValidateJobPaused(jobKey);
        await ValidateJobNotRunning(jobKey);

        var info = await Scheduler.GetJobDetail(jobKey);
        if (info == null) { return; }
        var oldAuthor = JobHelper.GetJobAuthor(info);
        request.Author = request.Author?.Trim() ?? string.Empty;
        info.JobDataMap.Put(Consts.Author, request.Author);

        // Reschedule job
        var triggers = await Scheduler.GetTriggersOfJob(jobKey);
        MonitorUtil.Lock(jobKey, lockSeconds: 3, MonitorEvents.JobAdded, MonitorEvents.JobPaused);

        try
        {
            // Schedule Job
            await Scheduler.ScheduleJob(info, triggers, true);
        }
        catch (Exception ex)
        {
            ValidateTriggerNeverFire(ex);
            throw;
        }

        AuditJobSafe(jobKey, $"set job author from '{oldAuthor}' to '{request.Author}'");

        // Pause job
        await Scheduler.PauseJob(jobKey);
    }

    public async Task SetAutoResume(PauseResumeJobRequest request)
    {
        if (request.AutoResumeDate == null)
        {
            throw new RestValidationException(nameof(PauseResumeJobRequest.AutoResumeDate), "auto resume date is null");
        }

        var jobKey = await JobKeyHelper.GetJobKey(request);
        ValidateSystemJob(jobKey);

        var isActive = await GetJobActiveMode(jobKey);
        if (isActive == JobActiveMembers.Active)
        {
            throw new RestValidationException("id", "all job triggers are active. there is no trigger to auto resume");
        }

        if (isActive == JobActiveMembers.NoTrigger)
        {
            throw new RestValidationException("id", "job has no triggers to auto resume");
        }

        await CancelQueuedResumeJob(jobKey);
        await AutoResumeJobUtil.QueueResumeJob(Scheduler, jobKey, request.AutoResumeDate.Value, AutoResumeTypes.AutoResume);
        AuditJobSafe(jobKey, "schedule auto resume", new { autoResumeDate = request.AutoResumeDate.Value });
    }

    public async Task CancelAutoResume(string id)
    {
        var jobKey = await JobKeyHelper.GetJobKey(id);
        ValidateSystemJob(jobKey);
        var deleted = await CancelQueuedResumeJob(jobKey);
        if (!deleted)
        {
            throw new RestNotFoundException("no auto resume exists for job");
        }
    }
}