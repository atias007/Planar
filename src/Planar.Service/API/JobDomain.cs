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
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public partial class JobDomain : BaseJobBL<JobDomain, JobData>
    {
        public JobDomain(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        #region Data

        public async Task RemoveData(string id, string key)
        {
            var info = await GetJobDetailsForDataCommands(id, key);
            if (info.JobDetails == null) { return; }

            ValidateDataKeyExists(info.JobDetails, key, id);
            var auditValue = PlanarConvert.ToString(info.JobDetails.JobDataMap[key]);
            info.JobDetails.JobDataMap.Remove(key);
            var triggers = await Scheduler.GetTriggersOfJob(info.JobKey);

            // Reschedule job
            MonitorUtil.LockJobEvent(info.JobKey, lockSeconds: 3, MonitorEvents.JobAdded, MonitorEvents.JobPaused);
            await Scheduler.ScheduleJob(info.JobDetails, triggers, true);
            await Scheduler.PauseJob(info.JobKey);

            AuditJobSafe(info.JobKey, $"remove job data with key '{key}'", new { value = auditValue?.Trim() });
        }

        public async Task PutData(JobOrTriggerDataRequest request, PutMode mode)
        {
            var info = await GetJobDetailsForDataCommands(request.Id, request.DataKey);
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

                info.JobDetails.JobDataMap.Put(request.DataKey, request.DataValue);
                AuditJobSafe(info.JobKey, $"add job data with key '{request.DataKey}'", new { value = request.DataValue?.Trim() });
            }

            var triggers = await Scheduler.GetTriggersOfJob(info.JobKey);

            // Reschedule job
            MonitorUtil.LockJobEvent(info.JobKey, lockSeconds: 3, MonitorEvents.JobAdded, MonitorEvents.JobPaused);
            await Scheduler.ScheduleJob(info.JobDetails, triggers, true);

            // Pause job
            await Scheduler.PauseJob(info.JobKey);
        }

        private async Task<DataCommandDto> GetJobDetailsForDataCommands(string jobId, string key)
        {
            // Get Job
            var jobKey = await JobKeyHelper.GetJobKey(jobId);
            var result = new DataCommandDto
            {
                JobKey = jobKey,
                JobDetails = await JobKeyHelper.ValidateJobExists(jobKey)
            };

            ValidateSystemJob(jobKey);
            ValidateSystemDataKey(key);
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

        public async Task<JobDetails> Get(string id)
        {
            var jobKey = await JobKeyHelper.GetJobKey(id);
            var info =
                await Scheduler.GetJobDetail(jobKey) ??
                throw new RestNotFoundException($"job with key '{KeyHelper.GetKeyTitle(jobKey)}' does not exist");

            var result = new JobDetails();
            await MapJobDetails(info, result);

            var triggers = await GetTriggersDetails(jobKey);
            result.SimpleTriggers = triggers.SimpleTriggers;
            result.CronTriggers = triggers.CronTriggers;

            return result;
        }

        public async Task<PagingResponse<JobBasicDetails>> GetAll(GetAllJobsRequest request)
        {
            var jobs = new List<IJobDetail>();

            foreach (var jobKey in await GetJobKeys(request))
            {
                var info = await Scheduler.GetJobDetail(jobKey);
                if (info == null) { continue; }
                jobs.Add(info);
            }

            if (!string.IsNullOrEmpty(request.JobType))
            {
                jobs = jobs
                    .Where(r => string.Equals(SchedulerUtil.GetJobTypeName(r.JobType), request.JobType, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (request.Active.HasValue)
            {
                jobs = jobs.Where(r => IsActiveJob(r.Key).Result == request.Active.Value).ToList();
            }

            var result = jobs
                .Select(SchedulerUtil.MapJobRowDetails)
                .SetPaging(request)
                .OrderBy(j => j.Group)
                .ThenBy(j => j.Name)
                .ToList();

            return new PagingResponse<JobBasicDetails>(request, result, jobs.Count);
        }

        public async Task<IEnumerable<AvailableJobToAdd>> GetAvailableJobsToAdd()
        {
            var result = new List<AvailableJobToAdd>();
            var folder = ServiceUtil.GetJobsFolder();
            var files = Directory.GetFiles(folder, FolderConsts.JobFileName, SearchOption.AllDirectories);
            foreach (var f in files)
            {
                var job = await GetAvailableJobToAdd(f, folder);
                if (job != null) { result.Add(job); }
            }

            return result;
        }

        private async Task<AvailableJobToAdd?> GetAvailableJobToAdd(string filename, string jobsFolder)
        {
            try
            {
                var request = GetJobDynamicRequestFromFilename(filename);
                if (request == null) { return null; }

                var key = JobKeyHelper.GetJobKey(request);
                if (key == null) { return null; }
                var details = await Scheduler.GetJobDetail(key);
                if (details == null)
                {
                    var fullFolder = new FileInfo(filename).Directory;
                    if (fullFolder == null) { return null; }
                    var relativeFolder = fullFolder.FullName[(jobsFolder.Length + 1)..];
                    var result = new AvailableJobToAdd(relativeFolder, fullFolder.Name);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Fail to get avaliable job folder info");
            }

            return null;
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

        public static IEnumerable<string> GetJobTypes()
        {
            return ServiceUtil.JobTypes;
        }

        public string GetJobFileTemplate(string typeName)
        {
            var assembly =
                Assembly.Load(typeName) ??
                throw new RestNotFoundException($"type '{typeName}' could not be found");

            var resources = assembly.GetManifestResourceNames();
            var resourceName =
                Array.Find(resources, r => r.ToLower() == $"{typeName}.JobFile.yml".ToLower()) ??
                throw new RestNotFoundException($"type '{typeName}' could not be found");

            using Stream? stream = assembly.GetManifestResourceStream(resourceName) ?? throw new RestNotFoundException("jobfile.yml resource could not be found");
            using StreamReader reader = new(stream);
            var result = reader.ReadToEnd();

            if (string.IsNullOrEmpty(result))
            {
                throw new RestNotFoundException("jobfile.yml resource could not be found");
            }

            return result;
        }

        public async Task<LastInstanceId?> GetLastInstanceId(string id, DateTime invokeDate)
        {
            var jobKey = await JobKeyHelper.GetJobKey(id);

            if (Helpers.JobKeyHelper.IsSystemJobKey(jobKey))
            {
                throw new RestValidationException("id", "this is system job and it does not have instance id");
            }

            var dal = Resolve<HistoryData>();
            var result = await dal.GetLastInstanceId(jobKey, invokeDate);
            return result;
        }

        public async Task<PagingResponse<JobAuditDto>> GetJobAudits(string id, PagingRequest paging)
        {
            var jobKey = await JobKeyHelper.GetJobKey(id);
            var jobId = await JobKeyHelper.GetJobId(jobKey) ?? string.Empty;
            var query = DataLayer.GetJobAudits(jobId);
            var result = await query.ProjectToWithPagingAsyc<JobAudit, JobAuditDto>(Mapper, paging);
            return result;
        }

        public async Task<PagingResponse<JobAuditDto>> GetAudits(PagingRequest request)
        {
            var query = DataLayer.GetAudits();
            var result = await query.ProjectToWithPagingAsyc<JobAudit, JobAuditDto>(Mapper, request);
            return result;
        }

        public async Task<JobAuditDto> GetJobAudit(int id)
        {
            var query = DataLayer.GetJobAudit(id);
            var entity = await Mapper.ProjectTo<JobAuditWithInfoDto>(query).FirstOrDefaultAsync();
            var result = ValidateExistingEntity(entity, "job audit");
            return result;
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
                result ??= new List<RunningJobDetails>();

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

        public async Task<GetTestStatusResponse> GetTestStatus(int id)
        {
            var dal = Resolve<HistoryData>();
            var result = await dal.GetTestStatus(id);
            return result ?? throw new RestNotFoundException($"test with id {id} not found");
        }

        public async Task Invoke(InvokeJobRequest request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);

            if (request.NowOverrideValue.HasValue)
            {
                var job = await Scheduler.GetJobDetail(jobKey);
                if (job == null) { return; }

                job.JobDataMap.Add(Consts.NowOverrideValue, request.NowOverrideValue.Value.ToString());
                await Scheduler.TriggerJob(jobKey, job.JobDataMap);
            }
            else
            {
                await Scheduler.TriggerJob(jobKey);
            }

            var auditInfo = request.NowOverrideValue.HasValue ? new { NowOverrideValue = request.NowOverrideValue.Value } : null;
            AuditJobSafe(jobKey, "job manually invoked", auditInfo);
        }

        public async Task QueueInvoke(QueueInvokeJobRequest request)
        {
            // build new job
            var jobKey = await JobKeyHelper.GetJobKey(request);
            ValidateSystemJob(jobKey);
            var job = await Scheduler.GetJobDetail(jobKey);
            if (job == null) { return; }

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

            // schedule trigger
            await Scheduler.ScheduleJob(newTrigger.Build());

            AuditJobSafe(jobKey, "job queue invoked", request);
        }

        public async Task Pause(JobOrTriggerKey request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            await Scheduler.PauseJob(jobKey);

            AuditJobSafe(jobKey, "job paused");
        }

        public async Task PauseAll()
        {
            await Scheduler.PauseAll();
            AuditJobsSafe("all jobs paused");
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
                Logger.LogError(ex, "Fail to delete properties after delete job id {Id}", id);
            }

            try
            {
                await DataLayer.DeleteJobAudit(jobId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Fail to delete audit after delete job id {Id}", id);
            }

            try
            {
                await DeleteMonitorOfJob(jobKey);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Fail to delete monitor after delete job id {Id}", id);
            }

            try
            {
                await DeleteJobStatistics(jobId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Fail to delete job statistics after delete job id {Id}", id);
            }
        }

        public async Task Resume(JobOrTriggerKey request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            await Scheduler.ResumeJob(jobKey);
            AuditJobSafe(jobKey, "job resumed");
        }

        public async Task ResumeAll()
        {
            await Scheduler.ResumeAll();
            AuditJobsSafe("all jobs resumed");
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

        private async Task<bool> IsActiveJob(JobKey jobKey)
        {
            var triggers = await Scheduler.GetTriggersOfJob(jobKey);
            if (triggers == null) { return false; }

            foreach (var t in triggers)
            {
                if (t.Key.Group == Consts.RecoveringJobsGroup) { continue; }
                var state = await Scheduler.GetTriggerState(t.Key);
                if (IaActiveTriggerState(state))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IaActiveTriggerState(TriggerState state)
        {
            return state != TriggerState.None && state != TriggerState.Paused;
        }

        private static void ValidateDataKeyExists(IJobDetail details, string key, string jobId)
        {
            if (details == null || !details.JobDataMap.ContainsKey(key))
            {
                throw new RestValidationException($"{key}", $"data with Key '{key}' could not found in job '{jobId}' (Name '{details?.Key.Name}' and Group '{details?.Key.Group}')");
            }
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

        private async Task DeleteJobStatistics(string jobId)
        {
            var dal = Resolve<MetricsData>();
            var s1 = new JobDurationStatistic { JobId = jobId };
            await dal.DeleteJobStatistic(s1);
            var s2 = new JobEffectedRowsStatistic { JobId = jobId };
            await dal.DeleteJobStatistic(s2);
        }

        private async Task<IReadOnlyCollection<JobKey>> GetJobKeys(GetAllJobsRequest request)
        {
            var matcher =
                string.IsNullOrEmpty(request.Group) ?
                GroupMatcher<JobKey>.AnyGroup() :
                GroupMatcher<JobKey>.GroupEquals(request.Group);

            switch (request.Filter)
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

        private async Task<bool> JobGroupExists(string jobGroup)
        {
            var allGroups = await Scheduler.GetJobGroupNames();
            return allGroups.Contains(jobGroup);
        }

        private async Task MapJobDetails(IJobDetail source, JobDetails target, JobDataMap? dataMap = null)
        {
            dataMap ??= source.JobDataMap;

            SchedulerUtil.MapJobRowDetails(source, target);
            target.Concurrent = !source.ConcurrentExecutionDisallowed;
            target.Author = JobHelper.GetJobAuthor(source);
            target.LogRetentionDays = JobHelper.GetLogRetentionDays(source);
            target.Durable = source.Durable;
            target.RequestsRecovery = source.RequestsRecovery;
            target.DataMap = Global.ConvertDataMapToDictionary(dataMap);
            target.Properties = await DataLayer.GetJobProperty(target.Id) ?? string.Empty;
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
    }
}