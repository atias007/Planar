using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.API.Helpers;
using Planar.Service.Exceptions;
using Planar.Service.General;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public partial class JobDomain : BaseBL<JobDomain>
    {
        public JobDomain(ILogger<JobDomain> logger, IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        public async Task ClearData(string id)
        {
            var jobKey = await JobKeyHelper.GetJobKey(id);
            var info = await Scheduler.GetJobDetail(jobKey);
            await ValidateJobNotRunning(jobKey);
            await Scheduler.PauseJob(jobKey);

            if (info != null)
            {
                info.JobDataMap.Clear();
                var triggers = await Scheduler.GetTriggersOfJob(jobKey);
                await Scheduler.ScheduleJob(info, triggers, true);
            }

            await Scheduler.ResumeJob(jobKey);
        }

        public async Task<JobDetails> Get(string id)
        {
            var jobKey = await JobKeyHelper.GetJobKey(id);
            var info = await Scheduler.GetJobDetail(jobKey);

            var result = new JobDetails();
            MapJobDetails(info, result);

            var triggers = await GetTriggersDetails(jobKey);
            result.SimpleTriggers = triggers.SimpleTriggers;
            result.CronTriggers = triggers.CronTriggers;

            return result;
        }

        private static async Task<IReadOnlyCollection<JobKey>> GetJobKeys(AllJobsMembers members)
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

        public async Task<LastInstanceId> GetLastInstanceId(string id, DateTime invokeDate)
        {
            var jobKey = await JobKeyHelper.GetJobKey(id);
            var result = await DataLayer.GetLastInstanceId(jobKey, invokeDate);
            return result;
        }

        public async Task<List<RunningJobDetails>> GetRunning()
        {
            var result = await SchedulerUtil.GetRunningJobs();
            if (AppSettings.Clustering)
            {
                var clusterResult = await new ClusterUtil(DataLayer, Logger).GetRunningJobs();
                if (result == null)
                {
                    result = new List<RunningJobDetails>();
                }

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
                result = await new ClusterUtil(DataLayer, Logger).GetRunningJob(instanceId);
            }

            if (result == null)
            {
                throw new RestNotFoundException();
            }

            return result;
        }

        public async Task<GetRunningInfoResponse> GetRunningInfo(string instanceId)
        {
            var result = await SchedulerUtil.GetRunningInfo(instanceId);
            if (result != null)
            {
                return result;
            }

            if (AppSettings.Clustering)
            {
                result = await new ClusterUtil(DataLayer, Logger).GetRunningInfo(instanceId);
            }

            if (result == null)
            {
                throw new RestNotFoundException($"instanceId {instanceId} was not found");
            }

            return result;
        }

        public async Task<Dictionary<string, string>> GetSettings(string id)
        {
            var result = new Dictionary<string, string>();
            var jobkey = await JobKeyHelper.GetJobKey(id);
            var details = await Scheduler.GetJobDetail(jobkey);
            var json = details?.JobDataMap[Consts.JobTypeProperties] as string;

            if (string.IsNullOrEmpty(json)) return result;
            var list = DeserializeObject<Dictionary<string, string>>(json);
            if (list == null) return result;
            if (list.ContainsKey("JobPath") == false) return result;
            var jobPath = list["JobPath"];

            var parameters = Global.Parameters;
            var settings = CommonUtil.LoadJobSettings(jobPath);
            result = parameters.Merge(settings);

            return result;
        }

        public async Task<GetTestStatusResponse> GetTestStatus(int id)
        {
            var result = await DataLayer.GetTestStatus(id);
            return result;
        }

        public async Task Invoke(InvokeJobRequest request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);

            if (request.NowOverrideValue.HasValue)
            {
                var job = await Scheduler.GetJobDetail(jobKey);
                if (job != null)
                {
                    job.JobDataMap.Add(Consts.NowOverrideValue, request.NowOverrideValue.Value);
                    await Scheduler.TriggerJob(jobKey, job.JobDataMap);
                }
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
                await DeleteMonitorOfJob(jobId, jobKey.Group);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Fail to delete monitor after delete job id {Id}", id);
            }
        }

        public async Task RemoveData(string id, string key)
        {
            var jobKey = await JobKeyHelper.GetJobKey(id);
            ValidateSystemJob(jobKey);
            ValidateSystemDataKey(key);
            var info = await Scheduler.GetJobDetail(jobKey);
            await ValidateJobNotRunning(jobKey);
            ValidateDataKeyExists(info, key, id);
            await Scheduler.PauseJob(jobKey);
            info.JobDataMap.Remove(key);
            var triggers = await Scheduler.GetTriggersOfJob(jobKey);
            await Scheduler.ScheduleJob(info, triggers, true);
            await Scheduler.ResumeJob(jobKey);
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
            if (AppSettings.Clustering && stop == false)
            {
                stop = await new ClusterUtil(DataLayer, Logger).StopRunningJob(request.FireInstanceId);
            }

            return stop;
        }

        public async Task UpdateProperty(UpsertJobPropertyRequest request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            ValidateSystemJob(jobKey);
            await ValidateJobNotRunning(jobKey);

            await Scheduler.PauseJob(jobKey);
            var info = await Scheduler.GetJobDetail(jobKey);
            var properties = GetJobProperties(info);

            if (properties.ContainsKey(request.PropertyKey))
            {
                properties[request.PropertyKey] = request.PropertyValue;
            }
            else
            {
                throw new RestValidationException($"{request.PropertyKey}", $"Property {request.PropertyKey} could not be found in job {request.Id}");
            }

            SetJobProperties(info, properties);

            var triggers = await Scheduler.GetTriggersOfJob(jobKey);
            await Scheduler.ScheduleJob(info, triggers, true);
            await Scheduler.ResumeJob(jobKey);
        }

        public async Task UpsertData(JobDataRequest request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            ValidateSystemJob(jobKey);
            ValidateSystemDataKey(request.DataKey);
            var info = await Scheduler.GetJobDetail(jobKey);
            if (info != null)
            {
                await ValidateJobNotRunning(jobKey);
                await Scheduler.PauseJob(jobKey);

                if (info.JobDataMap.ContainsKey(request.DataKey))
                {
                    info.JobDataMap.Put(request.DataKey, request.DataValue);
                }
                else
                {
                    info.JobDataMap.Add(request.DataKey, request.DataValue);
                }

                var triggers = await Scheduler.GetTriggersOfJob(jobKey);
                await Scheduler.ScheduleJob(info, triggers, true);
                await Scheduler.ResumeJob(jobKey);
            }
        }

        private static Dictionary<string, string> GetJobProperties(IJobDetail job)
        {
            var propertiesJson = Convert.ToString(job.JobDataMap[Consts.JobTypeProperties]);
            Dictionary<string, string> properties;
            if (string.IsNullOrEmpty(propertiesJson))
            {
                properties = new Dictionary<string, string>();
            }
            else
            {
                try
                {
                    properties = DeserializeObject<Dictionary<string, string>>(propertiesJson);
                }
                catch
                {
                    properties = new Dictionary<string, string>();
                }
            }

            return properties;
        }

        private static async Task<TriggerRowDetails> GetTriggersDetails(JobKey jobKey)
        {
            var result = new TriggerRowDetails();
            var triggers = await Scheduler.GetTriggersOfJob(jobKey);

            foreach (var t in triggers)
            {
                if (t is ISimpleTrigger t1)
                {
                    result.SimpleTriggers.Add(MapSimpleTriggerDetails(t1));
                }
                else
                {
                    if (t is ICronTrigger t2)
                    {
                        result.CronTriggers.Add(MapCronTriggerDetails(t2));
                    }
                }
            }

            return result;
        }

        private static async Task<bool> JobGroupExists(string jobGroup)
        {
            var allGroups = await Scheduler.GetJobGroupNames();
            return allGroups.Contains(jobGroup);
        }

        private static CronTriggerDetails MapCronTriggerDetails(ICronTrigger source)
        {
            var result = new CronTriggerDetails();
            MapTriggerDetails(source, result);
            result.CronExpression = source.CronExpressionString;
            return result;
        }

        private static void MapJobDetails(IJobDetail source, JobDetails target, JobDataMap dataMap = null)
        {
            if (dataMap == null)
            {
                dataMap = source.JobDataMap;
            }

            SchedulerUtil.MapJobRowDetails(source, target);
            target.Concurrent = !source.ConcurrentExecutionDisallowed;
            target.Durable = source.Durable;
            target.RequestsRecovery = source.RequestsRecovery;
            target.DataMap = Global.ConvertDataMapToDictionary(dataMap);

            if (dataMap.ContainsKey(Consts.JobTypeProperties))
            {
                var json = Convert.ToString(dataMap[Consts.JobTypeProperties]);
                if (string.IsNullOrEmpty(json) == false)
                {
                    var dict = DeserializeObject<Dictionary<string, string>>(json);
                    target.Properties = new SortedDictionary<string, string>(dict);
                }
            }
        }

        private static SimpleTriggerDetails MapSimpleTriggerDetails(ISimpleTrigger source)
        {
            var result = new SimpleTriggerDetails();
            MapTriggerDetails(source, result);
            result.RepeatCount = source.RepeatCount;
            result.RepeatInterval =
                source.RepeatInterval.TotalHours < 24 ?
                $"{source.RepeatInterval:hh\\:mm\\:ss}" :
                $"{source.RepeatInterval:\\(d\\)\\ hh\\:mm\\:ss}";

            result.TimesTriggered = source.TimesTriggered;
            return result;
        }

        private static void MapTriggerDetails(ITrigger source, TriggerDetails target)
        {
            target.CalendarName = source.CalendarName;
            if (TimeSpan.TryParse(Convert.ToString(source.JobDataMap[Consts.RetrySpan]), out var span))
            {
                target.RetrySpan = span;
            }

            target.Description = source.Description;
            target.End = source.EndTimeUtc?.LocalDateTime;
            target.Start = source.StartTimeUtc.LocalDateTime;
            target.FinalFire = source.FinalFireTimeUtc?.LocalDateTime;
            target.Group = source.Key.Group;
            target.MayFireAgain = source.GetMayFireAgain();
            target.MisfireBehaviour = source.MisfireInstruction.ToString();
            target.Name = source.Key.Name;
            target.NextFireTime = source.GetNextFireTimeUtc()?.LocalDateTime;
            target.PreviousFireTime = source.GetPreviousFireTimeUtc()?.LocalDateTime;
            target.Priority = source.Priority;
            target.DataMap = Global.ConvertDataMapToDictionary(source.JobDataMap);
            target.State = Scheduler.GetTriggerState(source.Key).Result.ToString();
            target.Id = TriggerKeyHelper.GetTriggerId(source);

            if (source.Key.Group == Consts.RecoveringJobsGroup)
            {
                target.Id = Consts.RecoveringJobsGroup;
            }
        }

        private static void SetJobProperties(IJobDetail job, Dictionary<string, string> properties)
        {
            var propertiesJson = SerializeObject(properties);

            if (job.JobDataMap.ContainsKey(Consts.JobTypeProperties))
            {
                job.JobDataMap.Put(Consts.JobTypeProperties, propertiesJson);
            }
            else
            {
                job.JobDataMap.Add(Consts.JobTypeProperties, propertiesJson);
            }
        }

        private static void ValidateDataKeyExists(IJobDetail details, string key, string jobId)
        {
            if (details == null || details.JobDataMap.ContainsKey(key) == false)
            {
                throw new RestValidationException($"{key}", $"data with Key '{key}' could not found in job '{jobId}' (Name '{details?.Key.Name}' and Group '{details?.Key.Group}')");
            }
        }

        private async Task ValidateJobNotRunning(JobKey jobKey)
        {
            var isRunning = await SchedulerUtil.IsJobRunning(jobKey);
            if (AppSettings.Clustering)
            {
                isRunning = isRunning && await new ClusterUtil(DataLayer, Logger).IsJobRunning(jobKey);
            }

            if (isRunning)
            {
                throw new RestValidationException($"{jobKey.Group}.{jobKey.Name}", $"job with name: {jobKey.Name} and group: {jobKey.Group} is currently running");
            }
        }

        private static void ValidateSystemDataKey(string key)
        {
            if (key.StartsWith(Consts.ConstPrefix))
            {
                throw new RestValidationException("key", "forbidden: this is system data key and it should not be modified");
            }
        }

        private static void ValidateSystemJob(JobKey jobKey)
        {
            if (jobKey.Group == Consts.PlanarSystemGroup)
            {
                throw new RestValidationException("key", "forbidden: this is system job and it should not be modified");
            }
        }

        private async Task DeleteMonitorOfJob(string jobId, string jobGroup)
        {
            await DataLayer.DeleteMonitorByJobId(jobId);
            if (await JobGroupExists(jobGroup) == false)
            {
                await DataLayer.DeleteMonitorByJobGroup(jobGroup);
            }
        }
    }
}