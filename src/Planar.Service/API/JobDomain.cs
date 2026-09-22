using CloudNative.CloudEvents;
using FluentValidation;
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
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Planar.Service.API;

public partial class JobDomain(
    IServiceProvider serviceProvider,
    IServiceScopeFactory scopeFactory)
    : BaseJobBL<JobDomain, IJobData>(serviceProvider), IJobActions
{
    private static readonly TimeSpan _longPullingSpan = TimeSpan.FromMinutes(5);
    private const string kind1 = "job";
    private const string kind2 = "job data";

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

        var scheduler = await GetScheduler();

        var pausedTriggers = await GetPausedTriggers(info.JobKey);
        await scheduler.PauseJob(info.JobKey);

        try
        {
            var triggers = await scheduler.GetTriggersOfJob(info.JobKey);

            // Reschedule job
            MonitorUtil.Lock(info.JobKey, lockSeconds: 3, MonitorEvents.JobAdded, MonitorEvents.JobPaused);
            await scheduler.ScheduleJob(info.JobDetails, triggers, true);
        }
        finally
        {
            await PauseTriggers(info.JobKey, pausedTriggers);
        }

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

            info.JobDetails.JobDataMap[request.DataKey] = request.DataValue ?? string.Empty;
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

            info.JobDetails.JobDataMap[request.DataKey] = request.DataValue ?? string.Empty;
            AuditJobSafe(info.JobKey, $"add job data with key '{request.DataKey}'", new { value = request.DataValue?.Trim() });
        }

        var pausedTriggers = await GetPausedTriggers(info.JobKey);
        var scheduler = await GetScheduler();
        await scheduler.PauseJob(info.JobKey);

        try
        {
            var triggers = await scheduler.GetTriggersOfJob(info.JobKey);

            // Reschedule job
            MonitorUtil.Lock(info.JobKey, lockSeconds: 3, MonitorEvents.JobAdded, MonitorEvents.JobPaused);
            await scheduler.ScheduleJob(info.JobDetails, triggers, true);
        }
        finally
        {
            await PauseTriggers(info.JobKey, pausedTriggers);
        }
    }

    internal async Task ApplyData(JobDataRequest request)
    {
        if (request.JobDetail == null) { return; }

        foreach (var data in request.JobData)
        {
            ApplyDataInner(request, data);
        }

        var triggerDomain = Resolve<TriggerDomain>();
        foreach (var t in request.TriggersData)
        {
            foreach (var data in t.Data)
            {
                triggerDomain.ApplyDataInner(t.Trigger, data);
            }
        }

        var pausedTriggers = await GetPausedTriggers(request.JobDetail.Key);
        var scheduler = await GetScheduler();
        await scheduler.PauseJob(request.JobDetail.Key);

        try
        {
            var triggers = await scheduler.GetTriggersOfJob(request.JobDetail.Key);

            // Reschedule job
            MonitorUtil.Lock(request.JobDetail.Key, lockSeconds: 3, MonitorEvents.JobAdded, MonitorEvents.JobPaused);
            await scheduler.ScheduleJob(request.JobDetail, triggers, true);
        }
        finally
        {
            await PauseTriggers(request.JobDetail.Key, pausedTriggers);
        }
    }

    private void ApplyDataInner(JobDataRequest request, KeyValuePair<string, string?> data)
    {
        if (request.JobDetail.JobDataMap.ContainsKey(data.Key))
        {
            request.JobDetail.JobDataMap[data.Key] = data.Value ?? string.Empty;
            AuditJobSafe(request.JobDetail.Key, $"update job data with key '{data.Key}'", new { value = data.Value?.Trim() });
        }
        else
        {
            var dataCount = CountUserJobDataItems(request.JobDetail.JobDataMap);
            if (dataCount >= Consts.MaximumJobDataItems)
            {
                throw new RestValidationException("job data", $"job data items exceeded maximum limit of {Consts.MaximumJobDataItems}");
            }

            request.JobDetail.JobDataMap[data.Key] = data.Value ?? string.Empty;
            AuditJobSafe(request.JobDetail.Key, $"add job data with key '{data.Key}'", new { value = data.Value?.Trim() });
        }
    }

    public async Task RemoveData(string id, string key)
    {
        var info = await GetJobDetailsForDataCommands(id, key);
        if (info.JobDetails == null) { return; }

        ValidateDataKeyExists(info.JobDetails, key, id);
        var auditValue = PlanarConvert.ToString(info.JobDetails.JobDataMap[key]);
        info.JobDetails.JobDataMap.Remove(key);

        var pausedTriggers = await GetPausedTriggers(info.JobKey);
        var scheduler = await GetScheduler();
        await scheduler.PauseJob(info.JobKey);

        try
        {
            var triggers = await scheduler.GetTriggersOfJob(info.JobKey);

            // Reschedule job
            MonitorUtil.Lock(info.JobKey, lockSeconds: 3, MonitorEvents.JobAdded, MonitorEvents.JobPaused);
            await scheduler.ScheduleJob(info.JobDetails, triggers, true);

            AuditJobSafe(info.JobKey, $"remove job data with key '{key}'", new { value = auditValue?.Trim() });
        }
        finally
        {
            await PauseTriggers(info.JobKey, pausedTriggers);
        }
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

        await ValidateJobNotRunning(jobKey);
        return result;
    }

    #endregion Data

    public enum PutMode
    {
        Add,
        Update
    }

    public async Task<ApplyResponse> Apply(HttpContext httpContext)
    {
        var yamls = await GetApplyYamls(httpContext, kind1, kind2);
        return await Apply(yamls, httpContext.RequestAborted);
    }

    public async Task<ApplyResponse> Apply(IEnumerable<KeyValuePair<string, string>> yamls, CancellationToken cancellationToken)
    {
        var jobYamls = yamls.Where(y => string.Equals(y.Key, kind1, StringComparison.OrdinalIgnoreCase));
        var jobDataYamls = yamls.Where(y => string.Equals(y.Key, kind2, StringComparison.OrdinalIgnoreCase));

        var jobRequests = jobYamls.Select(y => GetJobDynamicRequest(y.Value)).ToList();
        ValidateDuplicates(jobRequests);

        var dataRequests = await GetApplyEntities<JobDataRequest>(jobDataYamls, kind2, cancellationToken, withValidation: true);
        ValidateDuplicates(dataRequests);
        ValidateJobDataRequest(dataRequests);
        await FillDetails(dataRequests, cancellationToken);

        var response = new ApplyResponse();
        foreach (var item in jobRequests)
        {
            var result = await SafeApply(item);
            response.AddItem(result);
            cancellationToken.ThrowIfCancellationRequested();
        }

        foreach (var item in dataRequests)
        {
            await ApplyData(item);
            cancellationToken.ThrowIfCancellationRequested();
        }

        return response;
    }

    private async Task FillDetails(IReadOnlyCollection<JobDataRequest> dataRequests, CancellationToken cancellationToken)
    {
        foreach (var dataRequest in dataRequests)
        {
            var key = new JobOrTriggerKey { Id = $"{dataRequest.JobGroup}.{dataRequest.JobName}" };
            var jobKey = await JobKeyHelper.GetJobKey(key);
            var jobDetails = await JobKeyHelper.ValidateJobExists(jobKey);
            dataRequest.JobDetail = jobDetails;

            var scheduler = await GetScheduler();
            if (dataRequest.TriggersData.Count == 0) { continue; }
            var triggers = await scheduler.GetTriggersOfJob(jobKey, cancellationToken);
            foreach (var t in dataRequest.TriggersData)
            {
                var the_trigger = triggers.FirstOrDefault(tr => string.Equals(tr.Key.Name, t.Name, StringComparison.OrdinalIgnoreCase))
                    ?? throw new RestNotFoundException($"trigger with name '{t.Name}' does not exist for job '{dataRequest.JobGroup}.{dataRequest.JobName}'");

                t.Trigger = the_trigger;
            }
        }
    }

    private static void ValidateDuplicates(IReadOnlyCollection<SetJobDynamicRequest> requests)
    {
        var query = requests
           .GroupBy(r => new { r.Name, r.Group })
           .Where(g => g.Count() > 1)
           .Select(g => g.Key)
           .FirstOrDefault();

        if (query != null)
        {
            throw new RestValidationException("duplicate request", $"duplicate job request for name '{query.Name}' and group '{query.Group}'");
        }
    }

    private static void ValidateDuplicates(IReadOnlyCollection<JobDataRequest> requests)
    {
        var query = requests
           .GroupBy(r => new { r.JobName, r.JobGroup })
           .Where(g => g.Count() > 1)
           .Select(g => g.Key)
           .FirstOrDefault();

        if (query != null)
        {
            throw new RestValidationException("duplicate request", $"duplicate job data request for name '{query.JobName}' and group '{query.JobGroup}'");
        }
    }

    private static void ValidateJobDataRequest(IReadOnlyCollection<JobDataRequest> requests)
    {
        foreach (var item in requests)
        {
            item.JobData ??= [];

            #region Trim

            item.JobName = item.JobName?.SafeTrim() ?? string.Empty;
            item.JobGroup = item.JobGroup?.SafeTrim() ?? string.Empty;

            #endregion Trim

            #region Mandatory

            if (string.IsNullOrWhiteSpace(item.JobName)) throw new RestValidationException(name, "job name is mandatory");

            #endregion Mandatory

            #region Name & Group

            ValidateNameAndGroup(item.JobName, item.JobGroup);

            #endregion Name & Group

            ValidateDataMap(item.JobData, "job");
            foreach (var t in item.TriggersData)
            {
                t.Data ??= [];
                ValidateTriggerName(t.Name, null);
                ValidateRange(t.Name, 5, 50, name, trigger);
                if (t.Name != null && t.Name.StartsWith(Consts.RetryTriggerNamePrefix)) { throw new RestValidationException(name, $"trigger name '{t.Name}' has invalid prefix"); }
                ValidateDataMap(t.Data, trigger);
            }
        }
    }

    private async Task<ApplyResponseItem> SafeApply(SetJobDynamicRequest dynamicRequest)
    {
        // this is a safe operation
        var jobKey = JobKeyHelper.GetJobKey(dynamicRequest);

        try
        {
            var details = await JobKeyHelper.ValidateJobExists(jobKey);
            var wrapper = await Update(dynamicRequest, UpdateJobOptions.Default);
            var response =
                wrapper.Unchanged ?
                new ApplyResponseItem(wrapper.PlanarId.Id, ApplyAction.Unchanged, $"job {details.Key.Group}.{details.Key.Name} was unchanged", dynamicRequest.Source) :
                new ApplyResponseItem(wrapper.PlanarId.Id, ApplyAction.Update, $"job {details.Key.Group}.{details.Key.Name} updated", dynamicRequest.Source);

            if (!wrapper.Unchanged)
            {
                AuditJobSafe(details.Key, "job was applied (update)", response.Description);
            }

            return response;
        }
        catch (RestNotFoundException)
        {
            var response = await Add(dynamicRequest);
            var applyResponse = new ApplyResponseItem(response.Id, ApplyAction.Add, $"job {dynamicRequest.Group}.{dynamicRequest.Name} added", dynamicRequest.Source);

            if (jobKey != null)
            {
                AuditJobSafe(jobKey, "job was applied (add)", applyResponse.Description);
            }

            return applyResponse;
        }
    }

    private async Task<SetJobDynamicRequest> GetDynamicRequest(IJobFileRequest request)
    {
        await ValidateJobFileExists(request);
        var yml = await GetJobFileContent(request);
        var dynamicRequest = GetJobDynamicRequest(yml);
        return dynamicRequest;
    }

    private SetJobDynamicRequest GetDynamicRequest(string yml)
    {
        var dynamicRequest = GetJobDynamicRequest(yml);
        return dynamicRequest;
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
        var scheduler = await GetScheduler();

        var jobKey = await JobKeyHelper.GetJobKey(id);
        var info =
            await scheduler.GetJobDetail(jobKey) ??
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
        var resolver = Resolve<JobDetailsResolver>();
        IEnumerable<IJobDetail> jobs = (request.JobCategory switch
        {
            AllJobsMembers.AllUserJobs => await resolver.GetUserJobDetailsAsync(request.Group),
            AllJobsMembers.AllSystemJobs => await resolver.GetSystemJobDetailsAsync(),
            _ => await resolver.GetAllJobDetailsAsync(request.Group),
        });

        // filter by job type
        if (!string.IsNullOrEmpty(request.JobType))
        {
            jobs = jobs
                .Where(r => string.Equals(SchedulerUtil.GetJobTypeName(r.JobType), request.JobType, StringComparison.OrdinalIgnoreCase));
        }

        // filter by search
        if (!string.IsNullOrWhiteSpace(request.Filter))
        {
            jobs = jobs
                .Where(r =>
                    r.Key.Name.Contains(request.Filter, StringComparison.OrdinalIgnoreCase) ||
                    r.Key.Group.Contains(request.Filter, StringComparison.OrdinalIgnoreCase) ||
                    (r.Description != null && r.Description.Contains(request.Filter, StringComparison.OrdinalIgnoreCase))
                    );
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
        await using var monitorScope = scopeFactory.CreateAsyncScope();
        await using var historyScope = scopeFactory.CreateAsyncScope();
        await using var statisticsScope = scopeFactory.CreateAsyncScope();
        await using var jobScope = scopeFactory.CreateAsyncScope();
        await using var auditScope = scopeFactory.CreateAsyncScope();

        var monitorDomain = monitorScope.ServiceProvider.GetRequiredService<MonitorDomain>();
        var historyDomain = historyScope.ServiceProvider.GetRequiredService<HistoryDomain>();
        var statisticsDomain = statisticsScope.ServiceProvider.GetRequiredService<MetricsDomain>();
        var jobDomain = jobScope.ServiceProvider.GetRequiredService<JobDomain>();
        var auditDomain = auditScope.ServiceProvider.GetRequiredService<JobDomain>();

        var historyRequest = new GetHistoryRequest { JobId = id, PageSize = 10 };
        var details = await jobDomain.Get(id);
        var monitorsTask = monitorDomain.GetByJob(id);
        var audit = await auditDomain.GetJobAudits(id, new PagingRequest(1, 10));
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

        public string? Name { get; set; } = null;
    }

    public async Task<string> GetJobFilename(string id)
    {
        var key = await JobKeyHelper.GetJobKey(id);
        var jobId = await JobKeyHelper.GetJobId(key);
        if (string.IsNullOrWhiteSpace(jobId)) { throw NotFound(id); }
        var (properties, _) = await DataLayer.GetJobProperty(jobId);
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
        var scheduler = await GetScheduler();
        var result = (await scheduler.GetJobGroupNames())
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
        var scheduler = await GetScheduler();
        var jobKey = await JobKeyHelper.GetJobKey(id);
        var triggers = await scheduler.GetTriggersOfJob(jobKey);
        DateTime? result = null;
        foreach (var t in triggers)
        {
            var state = await scheduler.GetTriggerState(t.Key);
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
        var scheduler = await GetScheduler();
        var jobKey = await JobKeyHelper.GetJobKey(id);
        var triggers = await scheduler.GetTriggersOfJob(jobKey);
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

        result = [.. result.Where(r => r.Group != Consts.PlanarSystemGroup)];

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
                await Task.Delay(500, cts.Token);
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

    public async Task Invoke(InvokeJobRequest request)
    {
        var jobKey = await JobKeyHelper.GetJobKey(request);
        ValidateDataMap(request.Data, "invoke");
        await ValidateSchedulerRunning();

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

        var scheduler = await GetScheduler();
        if (request.Data.Count != 0)
        {
            var data = new JobDataMap(request.Data);
            await scheduler.TriggerJob(jobKey, data);
        }
        else
        {
            await scheduler.TriggerJob(jobKey);
        }

        AuditJobSafe(jobKey, "job manually invoked", request);
    }

    public async Task Pause(PauseResumeJobRequest request)
    {
        var jobKey = await JobKeyHelper.GetJobKey(request);
        ValidateSystemJob(jobKey);

        var scheduler = await GetScheduler();
        await CancelQueuedResumeJob(jobKey);
        await scheduler.PauseJob(jobKey);
        SafeRefreshJobDetailsCache();

        if (request.AutoResumeDate == null)
        {
            Audit(false, null);
            return;
        }

        // Handle auto resume
        var job = await scheduler.GetJobDetail(jobKey);
        if (job == null)
        {
            Audit(false, null);
            return;
        }

        await AutoResumeJobUtil.QueueResumeJob(scheduler, jobKey, request.AutoResumeDate.Value, AutoResumeTypes.AutoResume);
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
        var scheduler = await GetScheduler();
        var keys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(request.Name));
        if (keys.Count == 0)
        {
            throw new RestNotFoundException($"group '{request.Name}' was not found");
        }
        await scheduler.PauseJobs(GroupMatcher<JobKey>.GroupEquals(request.Name));
        SafeRefreshJobDetailsCache();

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
        var scheduler = await GetScheduler();
        var job = await scheduler.GetJobDetail(jobKey);
        if (job == null) { return new PlanarIdResponse(); }

        // build new trigger
        var triggerId = ServiceUtil.GenerateId();
        var triggerKey = new TriggerKey($"DueTo.{request.DueDate:yyyyMMdd.HHmmss}", Consts.QueueInvokeTriggerGroup);
        var exists = await scheduler.GetTrigger(triggerKey);
        if (exists != null)
        {
            throw new RestValidationException("due date", $"job already has queue invoke trigger with date {request.DueDate:yyyy-MM-dd HH:mm:ss}");
        }

        // Basic
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

        // Timeout
        if (request.Timeout.HasValue)
        {
            var timeoutValue = request.Timeout.Value.Ticks.ToString();
            newTrigger = newTrigger.UsingJobData(Consts.TriggerTimeout, timeoutValue);
        }

        request.Data ??= [];

        // Now override
        if (request.NowOverrideValue.HasValue)
        {
            request.Data.Add(Consts.NowOverrideValue, request.NowOverrideValue.Value.ToString());
        }

        // Data
        foreach (var item in request.Data)
        {
            newTrigger = newTrigger.UsingJobData(item.Key, item.Value ?? string.Empty);
        }

        // Retry span, Max retries
        if (request.RetrySpan.HasValue)
        {
            newTrigger = newTrigger.UsingJobData(Consts.RetrySpan, request.RetrySpan.Value.ToSimpleTimeString());
        }

        // Max retries
        if (request.MaxRetries.HasValue)
        {
            newTrigger = newTrigger.UsingJobData(Consts.MaxRetries, request.MaxRetries.Value.ToString());
        }

        try
        {
            // Schedule Job
            await scheduler.ScheduleJob(newTrigger.Build());
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
        await ValidateSchedulerRunning();
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

        var scheduler = await GetScheduler();
        await scheduler.DeleteJob(jobKey);
        AuditJobSafe(jobKey, "job deleted", null, jobId);
        SafeRemoveJobDetailsCache(jobKey);
        _ = SafeClearJobInfo(jobId, jobKey, id);
    }

    private async Task SafeClearJobInfo(string jobId, JobKey jobKey, string id)
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

        var scheduler = await GetScheduler();
        if (request.AutoResumeDate == null)
        {
            await scheduler.ResumeJob(jobKey);
            AuditJobSafe(jobKey, "job resumed");
            await CancelQueuedResumeJob(jobKey);
        }
        else
        {
            await AutoResumeJobUtil.QueueResumeJob(scheduler, jobKey, request.AutoResumeDate.Value, AutoResumeTypes.AutoResume);
            AuditJobSafe(jobKey, "schedule auto resume", new { autoResumeDate = request.AutoResumeDate.Value });
        }

        SafeRefreshJobDetailsCache();
    }

    public async Task ResumeGroup(PauseResumeGroupRequest request)
    {
        ValidateSystemGroup(request.Name);
        var scheduler = await GetScheduler();
        var keys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(request.Name));
        if (keys.Count == 0)
        {
            throw new RestNotFoundException($"group '{request.Name}' was not found");
        }

        await scheduler.ResumeJobs(GroupMatcher<JobKey>.GroupEquals(request.Name));
        SafeRefreshJobDetailsCache();

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
        await ValidateJobNotRunning(jobKey);

        var scheduler = await GetScheduler();
        var info = await scheduler.GetJobDetail(jobKey);
        if (info == null) { return; }

        var oldAuthor = JobHelper.GetJobAuthor(info);
        request.Author = request.Author?.Trim() ?? string.Empty;
        info.JobDataMap[Consts.Author] = request.Author;

        // Reschedule job
        var triggers = await scheduler.GetTriggersOfJob(jobKey);
        MonitorUtil.Lock(jobKey, lockSeconds: 3, MonitorEvents.JobAdded, MonitorEvents.JobPaused);

        var pausedTriggers = await GetPausedTriggers(info.Key);
        await scheduler.PauseJob(info.Key);

        try
        {
            // Schedule Job
            await scheduler.ScheduleJob(info, triggers, true);
        }
        catch (Exception ex)
        {
            ValidateTriggerNeverFire(ex);
            throw;
        }
        finally
        {
            await PauseTriggers(info.Key, pausedTriggers);
        }

        AuditJobSafe(jobKey, $"set job author from '{oldAuthor}' to '{request.Author}'");
        SafeRefreshJobDetailsCache();
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
        var scheduler = await GetScheduler();
        await AutoResumeJobUtil.QueueResumeJob(scheduler, jobKey, request.AutoResumeDate.Value, AutoResumeTypes.AutoResume);
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