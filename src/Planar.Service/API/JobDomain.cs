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
using Polly;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public partial class JobDomain(IServiceProvider serviceProvider) : BaseJobBL<JobDomain, JobData>(serviceProvider)
    {
        private static TimeSpan _longPullingSpan = TimeSpan.FromMinutes(5);

        #region Data

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

        public async Task<IEnumerable<string>> GetJobGroupNames()
        {
            var result = (await Scheduler.GetJobGroupNames())
                .Where(g => !string.Equals(g, Consts.PlanarSystemGroup, StringComparison.OrdinalIgnoreCase));
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
            var jobList = jobs.Select(j => MapJobDetailsSlim(j).Result).ToList();

            // filter by active
            if (request.Active.HasValue)
            {
                if (request.Active.Value)
                {
                    jobList = jobList.Where(r => r.Active != JobActiveMembers.Inactive).ToList();
                }
                else
                {
                    jobList = jobList.Where(r => r.Active == JobActiveMembers.Inactive).ToList();
                }
            }

            // paging & order by
            var result = jobList
                .Select(j => j)
                .SetPaging(request)
                .OrderBy(j => j.Group)
                .ThenBy(j => j.Name)
                .ToList();

            return new PagingResponse<JobBasicDetails>(request, result, jobs.Count);
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
            var monitorDomain = _serviceProvider.GetRequiredService<MonitorDomain>();
            var historyDomain = _serviceProvider.GetRequiredService<HistoryDomain>();
            var statisticsDomain = _serviceProvider.GetRequiredService<MetricsDomain>();

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

        public async Task<LastInstanceId?> GetLastInstanceId(string id, DateTime invokeDate, CancellationToken cancellationToken)
        {
            var jobKey = await JobKeyHelper.GetJobKey(id);

            if (JobKeyHelper.IsSystemJobKey(jobKey))
            {
                throw new RestValidationException("id", "this is system job and it does not have instance id");
            }

            var dal = Resolve<HistoryData>();

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
                if (result == null)
                {
                    result = nextDate;
                }
                else
                {
                    if (nextDate < result)
                    {
                        result = nextDate;
                    }
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
                if (result == null)
                {
                    result = prevDate;
                }
                else
                {
                    if (prevDate > result)
                    {
                        result = prevDate;
                    }
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
            var access = _serviceProvider.GetRequiredService<IHttpContextAccessor>();
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

        public async Task Pause(JobOrTriggerKey request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            ValidateSystemJob(jobKey);
            await Scheduler.PauseJob(jobKey);

            AuditJobSafe(jobKey, "job paused");
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

            try
            {
                AuditJobsSafe($"pause job group '{request.Name}'");
                foreach (var key in keys)
                {
                    AuditJobSafe(key, $"job paused while pause job group '{request.Name}'");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "fail to audit jobs while pause job group '{Name}'", request.Name);
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

            try
            {
                AuditJobsSafe($"resume job group '{request.Name}'");
                foreach (var key in keys)
                {
                    AuditJobSafe(key, $"job resume while resume job group '{request.Name}'");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "fail to audit jobs while resume group '{Name}'", request.Name);
            }
        }

        public async Task<PlanarIdResponse> QueueInvoke(QueueInvokeJobRequest request)
        {
            // build new job
            var jobKey = await JobKeyHelper.GetJobKey(request);
            ValidateSystemJob(jobKey);
            ValidateDataMap(request.Data, "queue invoke");

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

            if (request.Data != null && request.Data.Count != 0)
            {
                foreach (var item in request.Data)
                {
                    newTrigger = newTrigger.UsingJobData(item.Key, item.Value ?? string.Empty);
                }
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

            AuditJobSafe(jobKey, "job queue invoked", request);

            return new PlanarIdResponse { Id = triggerId };
        }

        public async Task Remove(string id)
        {
            var jobKey = await JobKeyHelper.GetJobKey(id);
            var jobId = await JobKeyHelper.GetJobId(jobKey) ?? string.Empty;
            ValidateSystemJob(jobKey);

            await Scheduler.DeleteJob(jobKey);

            try
            {
                await DataLayer.DeleteJobProperty(jobId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "fail to delete properties after delete job id {Id}", id);
            }

            try
            {
                await DataLayer.DeleteJobAudit(jobId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "fail to delete audit after delete job id {Id}", id);
            }

            try
            {
                await DeleteMonitorOfJob(jobKey);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "fail to delete monitor after delete job id {Id}", id);
            }

            try
            {
                await DeleteJobStatistics(jobId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "fail to delete job statistics after delete job id {Id}", id);
            }
        }

        public async Task Resume(JobOrTriggerKey request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            ValidateSystemJob(jobKey);
            await Scheduler.ResumeJob(jobKey);
            AuditJobSafe(jobKey, "job resumed");
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
            var dal = Resolve<MetricsData>();
            var s1 = new JobDurationStatistic { JobId = jobId };
            await dal.DeleteJobStatistic(s1);
            var s2 = new JobEffectedRowsStatistic { JobId = jobId };
            await dal.DeleteJobStatistic(s2);
        }

        private async Task DeleteMonitorOfJob(JobKey jobKey)
        {
            var dal = Resolve<MonitorData>();
            await dal.DeleteMonitorByJobId(jobKey.Group, jobKey.Name);
            if (!await JobGroupExists(jobKey.Group))
            {
                await dal.DeleteMonitorByJobGroup(jobKey.Group);
            }
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
                var relativeFolder = fullFolder.FullName[(jobsFolder.Length + 1)..];
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
            var triggers = await Scheduler.GetTriggersOfJob(jobKey);
            if (triggers == null || triggers.Count == 0) { return JobActiveMembers.Inactive; }

            var hasActive = false;
            var hasInactive = false;
            foreach (var t in triggers)
            {
                if (t.Key.Group == Consts.RecoveringJobsGroup) { continue; }
                var active = await IsTriggerActive(t);
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

        public async Task<bool> IsTriggerActive(ITrigger trigger)
        {
            var state = await Scheduler.GetTriggerState(trigger.Key);
            return TriggerHelper.IsActiveState(state);
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
            return target;
        }

        private async Task<JobDetails> MapJobDetails(IJobDetail source, JobDataMap? dataMap = null)
        {
            var target = new JobDetails();
            SchedulerUtil.MapJobRowDetails(source, target);
            target.Active = await GetJobActiveMode(source.Key);

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
                if (trigger.JobDataMap.TryGetDateTimeValueFromString("Created", out var created))
                {
                    target.CircuitBreaker.ActivatedAt = created;
                }
            }

            return target;
        }
    }
}