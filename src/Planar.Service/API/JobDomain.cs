using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Planar.API.Common.Entities;
using Planar.Common;
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Azure.Core.HttpHeader;

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
            info.JobDetails.JobDataMap.Remove(key);
            var triggers = await Scheduler.GetTriggersOfJob(info.JobKey);
            await Scheduler.ScheduleJob(info.JobDetails, triggers, true);
            await Scheduler.PauseJob(info.JobKey);
        }

        public async Task UpsertData(JobOrTriggerDataRequest request, UpsertMode mode)
        {
            var info = await GetJobDetailsForDataCommands(request.Id, request.DataKey);
            if (info.JobDetails == null) { return; }

            if (info.JobDetails.JobDataMap.ContainsKey(request.DataKey))
            {
                if (mode == UpsertMode.Add)
                {
                    throw new RestConflictException($"data with key '{request.DataKey}' already exists");
                }

                info.JobDetails.JobDataMap.Put(request.DataKey, request.DataValue);
            }
            else
            {
                if (mode == UpsertMode.Update)
                {
                    throw new RestNotFoundException($"data with key '{request.DataKey}' not found");
                }

                info.JobDetails.JobDataMap.Add(request.DataKey, request.DataValue);
            }

            var triggers = await Scheduler.GetTriggersOfJob(info.JobKey);
            await Scheduler.ScheduleJob(info.JobDetails, triggers, true);
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

        public enum UpsertMode
        {
            Add,
            Update,
            Upsert
        }

        public async Task<JobDetails> Get(string id)
        {
            var jobKey = await JobKeyHelper.GetJobKey(id);
            var info = await Scheduler.GetJobDetail(jobKey);

            var result = new JobDetails();
            await MapJobDetails(info, result);

            var triggers = await GetTriggersDetails(jobKey);
            result.SimpleTriggers = triggers.SimpleTriggers;
            result.CronTriggers = triggers.CronTriggers;

            return result;
        }

        public async Task<List<JobRowDetails>> GetAll(GetAllJobsRequest request)
        {
            var result = new List<JobRowDetails>();

            foreach (var jobKey in await GetJobKeys(request.Filter))
            {
                var info = await Scheduler.GetJobDetail(jobKey);
                var details = new JobRowDetails();
                SchedulerUtil.MapJobRowDetails(info, details);
                result.Add(details);
            }

            result = result
                .OrderBy(r => r.Group)
                .ThenBy(r => r.Name)
                .ToList();

            return result;
        }

        public async Task<IEnumerable<AvailableJobToAdd>> GetAvailableJobsToAdd()
        {
            var result = new List<AvailableJobToAdd>();
            var folder = ServiceUtil.GetJobsFolder();
            var files = Directory.GetFiles(folder, FolderConsts.JobFileName, SearchOption.AllDirectories);
            foreach (var f in files)
            {
                try
                {
                    var yml = File.ReadAllText(f);
                    var request = YmlUtil.Deserialize<SetJobDynamicRequest>(yml);
                    if (request == null) { continue; }

                    var key = JobKeyHelper.GetJobKey(request);
                    var details = await Scheduler.GetJobDetail(key);
                    if (details == null)
                    {
                        var fullFolder = new FileInfo(f).Directory;
                        var relativeFolder = fullFolder.FullName[(folder.Length + 1)..];
                        result.Add(new AvailableJobToAdd { RelativeFolder = relativeFolder, Name = fullFolder.Name });
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogDebug(ex, "Fail to get avaliable job folder info");
                }
            }

            return result;
        }

        public IEnumerable<string> GetJobFileTemplates()
        {
            return new[] { "PlanarJob", "ProcessJob" };
        }

        public string GetJobFileTemplate(string typeName)
        {
            var assembly = Assembly.Load(typeName);
            if (assembly == null)
            {
                throw new RestNotFoundException($"type '{typeName}' could not be found");
            }

            using Stream stream = assembly.GetManifestResourceStream($"{typeName}.JobFile.yml");
            {
                if (stream == null)
                {
                    throw new RestNotFoundException("JobFile.yml resource could not be found");
                }

                using StreamReader reader = new(stream);
                var result = reader.ReadToEnd();

                if (string.IsNullOrEmpty(result))
                {
                    throw new RestNotFoundException("JobFile.yml resource could not be found");
                }

                return result;
            }
        }

        public async Task<LastInstanceId> GetLastInstanceId(string id, DateTime invokeDate)
        {
            var jobKey = await JobKeyHelper.GetJobKey(id);
            var dal = Resolve<HistoryData>();
            var result = await dal.GetLastInstanceId(jobKey, invokeDate);
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
                var state = await Scheduler.GetTriggerState(t.Key);
                if (state == TriggerState.Paused) { continue; }
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
            if (AppSettings.Clustering)
            {
                var clusterResult = await ClusterUtil.GetRunningJobs();
                result ??= new List<RunningJobDetails>();

                if (clusterResult != null)
                {
                    result.AddRange(clusterResult);
                }
            }

            return result;
        }

        public async Task<RunningJobDetails> GetRunning(string instanceId)
        {
            var result = await SchedulerUtil.GetRunningJob(instanceId);
            if (result == null && AppSettings.Clustering)
            {
                result = await ClusterUtil.GetRunningJob(instanceId);
            }

            if (result == null)
            {
                throw new RestNotFoundException();
            }

            return result;
        }

        public async Task<GetRunningDataResponse> GetRunningData(string instanceId)
        {
            var result = await SchedulerUtil.GetRunningData(instanceId);
            if (result != null)
            {
                return result;
            }

            if (AppSettings.Clustering)
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
            var jobId = await JobKeyHelper.GetJobId(id);
            var properties = await DataLayer.GetJobProperty(jobId);

            if (string.IsNullOrEmpty(properties))
            {
                return result;
            }

            string jobPath;
            try
            {
                var pathObj = YmlUtil.Deserialize<JobPropertiesWithPath>(properties);
                jobPath = pathObj.Path;
            }
            catch (Exception)
            {
                return result;
            }

            var settings = JobSettingsLoader.LoadJobSettings(jobPath);
            result = settings.Select(d => new KeyValueItem(d.Key, d.Value)).ToList();

            return result;
        }

        public async Task<GetTestStatusResponse> GetTestStatus(int id)
        {
            var dal = Resolve<HistoryData>();
            var result = await dal.GetTestStatus(id);
            if (result == null)
            {
                throw new RestNotFoundException($"test with id {id} not found");
            }

            return result;
        }

        public async Task Invoke(InvokeJobRequest request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);

            if (request.NowOverrideValue.HasValue)
            {
                var job = await Scheduler.GetJobDetail(jobKey);
                job.JobDataMap.Add(Consts.NowOverrideValue, request.NowOverrideValue.Value);
                await Scheduler.TriggerJob(jobKey, job.JobDataMap);
            }
            else
            {
                await Scheduler.TriggerJob(jobKey);
            }
        }

        public async Task Pause(JobOrTriggerKey request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            await Scheduler.PauseJob(jobKey);
        }

        public async Task PauseAll()
        {
            await Scheduler.PauseAll();
        }

        public async Task Remove(string id)
        {
            var jobKey = await JobKeyHelper.GetJobKey(id);
            var jobId = await JobKeyHelper.GetJobId(jobKey);
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
                await DeleteMonitorOfJob(jobId, jobKey.Group);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Fail to delete monitor after delete job id {Id}", id);
            }
        }

        public async Task Resume(JobOrTriggerKey request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            await Scheduler.ResumeJob(jobKey);
        }

        public async Task ResumeAll()
        {
            await Scheduler.ResumeAll();
        }

        public async Task<bool> Stop(FireInstanceIdRequest request)
        {
            var stop = await SchedulerUtil.StopRunningJob(request.FireInstanceId);
            if (AppSettings.Clustering && !stop)
            {
                stop = await ClusterUtil.StopRunningJob(request.FireInstanceId);
            }

            if (!stop && !await SchedulerUtil.IsRunningInstanceExistOnLocal(request.FireInstanceId))
            {
                throw new RestNotFoundException($"instance id '{request.FireInstanceId}' is not running");
            }

            return stop;
        }

        private static void ValidateDataKeyExists(IJobDetail details, string key, string jobId)
        {
            if (details == null || !details.JobDataMap.ContainsKey(key))
            {
                throw new RestValidationException($"{key}", $"data with Key '{key}' could not found in job '{jobId}' (Name '{details?.Key.Name}' and Group '{details?.Key.Group}')");
            }
        }

        private async Task DeleteMonitorOfJob(string jobId, string jobGroup)
        {
            var key = await JobKeyHelper.GetJobKeyById(jobId);
            var dal = Resolve<MonitorData>();
            await dal.DeleteMonitorByJobId(key.Group, key.Name);
            if (!await JobGroupExists(jobGroup))
            {
                await dal.DeleteMonitorByJobGroup(jobGroup);
            }
        }

        private async Task<IReadOnlyCollection<JobKey>> GetJobKeys(AllJobsMembers members)
        {
            switch (members)
            {
                case AllJobsMembers.AllUserJobs:
                    var result = await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
                    var list = result.Where(x => x.Group != Consts.PlanarSystemGroup).ToList();
                    return new ReadOnlyCollection<JobKey>(list);

                case AllJobsMembers.AllSystemJobs:
                    return await Scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(Consts.PlanarSystemGroup));

                default:
                case AllJobsMembers.All:
                    return await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
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

        private async Task MapJobDetails(IJobDetail source, JobDetails target, JobDataMap dataMap = null)
        {
            dataMap ??= source.JobDataMap;

            SchedulerUtil.MapJobRowDetails(source, target);
            target.Concurrent = !source.ConcurrentExecutionDisallowed;
            target.Durable = source.Durable;
            target.RequestsRecovery = source.RequestsRecovery;
            target.DataMap = Global.ConvertDataMapToDictionary(dataMap);
            target.Properties = await DataLayer.GetJobProperty(target.Id);
        }
    }
}