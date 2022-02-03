using FluentValidation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planner.API.Common.Entities;
using Planner.Common;
using Planner.Service.API.Helpers;
using Planner.Service.API.Validation;
using Planner.Service.Data;
using Planner.Service.Exceptions;
using Planner.Service.General;
using Planner.Service.General.Password;
using Planner.Service.Model;
using Planner.Service.Monitor;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DbJobInstanceLog = Planner.Service.Model.JobInstanceLog;
using JobInstanceLog = Planner.API.Common.Entities.JobInstanceLog;

namespace Planner.Service.API
{
    public partial class DeamonBL : BaseBL
    {
        private readonly DataLayer _dal;
        private readonly ILogger<DeamonBL> _logger;

        public DeamonBL(DataLayer dal, ILogger<DeamonBL> logger)
        {
            _dal = dal ?? throw new NullReferenceException(nameof(dal));
            _logger = logger ?? throw new NullReferenceException(nameof(logger));
        }

        private static IScheduler Scheduler
        {
            get
            {
                return MainService.Scheduler;
            }
        }

        public static async Task<BaseResponse> ClearJobData(JobOrTriggerKey request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
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

            return BaseResponse.Empty;
        }

        public static async Task<GetAllJobsResponse> GetAllJobs()
        {
            var result = new List<JobRowDetails>();

            foreach (var jobKey in await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup()))
            {
                var info = await Scheduler.GetJobDetail(jobKey);

                var details = new JobRowDetails();
                MapJobRowDetails(info, details);
                result.Add(details);
            }

            result = result
                .OrderBy(r => r.Group)
                .ThenBy(r => r.Name)
                .ToList();

            var response = new GetAllJobsResponse(result);
            return response;
        }

        public static async Task<BaseResponse<TriggerRowDetails>> GetTriggersDetails(JobOrTriggerKey request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            var result = await GetTriggersDetails(jobKey);
            return result;
        }

        public static async Task<BaseResponse<TriggerRowDetails>> GetTriggerDetails(JobOrTriggerKey request)
        {
            var triggerKey = await TriggerKeyHelper.GetTriggerKey(request);
            var result = await GetTriggerDetails(triggerKey);
            return result;
        }

        public static async Task<BaseResponse<List<string>>> GetAllCalendars()
        {
            var list = (await Scheduler.GetCalendarNames()).ToList();
            return new BaseResponse<List<string>>(list);
        }

        public async Task<GetAllGlobalParametersResponse> GetAllGlobalParameters()
        {
            var data = (await _dal.GetAllGlobalParameter())
                .Select(p => GetGlobalParameterData(p))
                .ToDictionary(p => p.Key, p => p.Value);

            return new GetAllGlobalParametersResponse { Result = data };
        }

        public async Task<BaseResponse<string>> GetGlobalParameter(GlobalParameterKey request)
        {
            var data = await _dal.GetGlobalParameter(request.Key);
            return new BaseResponse<string>(data?.ParamValue);
        }

        public static async Task<BaseResponse<JobDetails>> GetJobDetails(JobOrTriggerKey request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            var info = await Scheduler.GetJobDetail(jobKey);

            var result = new JobDetails();
            MapJobDetails(info, result);

            var triggers = await GetTriggersDetails(jobKey);
            result.SimpleTriggers = triggers.Result.SimpleTriggers;
            result.CronTriggers = triggers.Result.CronTriggers;

            var response = new BaseResponse<JobDetails>(result);
            return response;
        }

        public static async Task<BaseResponse<Dictionary<string, string>>> GetJobSettings(JobOrTriggerKey request)
        {
            var result = new BaseResponse<Dictionary<string, string>>();
            var jobkey = await JobKeyHelper.GetJobKey(request);
            var details = await Scheduler.GetJobDetail(jobkey);
            var json = details?.JobDataMap[Consts.JobTypeProperties] as string;

            if (string.IsNullOrEmpty(json)) return result;
            var list = DeserializeObject<Dictionary<string, string>>(json);
            if (list == null) return result;
            if (list.ContainsKey("JobPath") == false) return result;
            var jobPath = list["JobPath"];

            var parameters = Global.Parameters;
            var settings = CommonUtil.LoadJobSettings(jobPath);
            result.Result = parameters.Merge(settings);

            return result;
        }

        public async Task<GetTraceResponse> GetTrace(GetTraceRequest request)
        {
            if (request.Rows.GetValueOrDefault() == 0) { request.Rows = 50; }
            var result = await _dal.GetTrace(request);

            return new GetTraceResponse { Result = result };
        }

        public async Task<BaseResponse<string>> GetTraceException(GetByIdRequest request)
        {
            var result = await _dal.GetTraceException(request.Id);
            return new BaseResponse<string>(result);
        }

        public async Task<BaseResponse<string>> GetTraceProperties(GetByIdRequest request)
        {
            var result = await _dal.GetTraceProperties(request.Id);
            return new BaseResponse<string>(result);
        }

        public static async Task<GetRunningJobsResponse> GetRunningJobs(FireInstanceIdRequest request)
        {
            var result = new List<RunningJobDetails>();

            foreach (var job in await Scheduler.GetCurrentlyExecutingJobs())
            {
                if (string.IsNullOrEmpty(request.FireInstanceId) || request.FireInstanceId == job.FireInstanceId)
                {
                    var details = new RunningJobDetails();
                    MapJobRowDetails(job.JobDetail, details);
                    MapJobExecutionContext(job, details);
                    result.Add(details);
                }
            }

            var response = new GetRunningJobsResponse(result.OrderBy(r => r.Name).ToList());
            return response;
        }

        public static async Task<BaseResponse> StopRunningJob(FireInstanceIdRequest request)
        {
            var result = await Scheduler.Interrupt(request.FireInstanceId);
            if (result == false)
            {
                throw new ValidationException($"Fail to stop running job with FireInstanceId {request.FireInstanceId}");
            }

            return BaseResponse.Empty;
        }

        public static async Task<BaseResponse<string>> GetRunningInfo(FireInstanceIdRequest request)
        {
            var job = (await Scheduler.GetCurrentlyExecutingJobs()).FirstOrDefault(j => j.FireInstanceId == request.FireInstanceId);
            var information = string.Empty;
            var exceptions = string.Empty;

            if (job != null)
            {
                information = JobExecutionMetadataUtil.GetInformation(job);
                exceptions = JobExecutionMetadataUtil.GetExceptionsText(job);
            }

            var obj = new { Information = information, Exceptions = exceptions };
            var response = SerializeObject(obj);
            return new BaseResponse<string>(response);
        }

        public static async Task<BaseResponse<RunningJobDetails>> GetRunningJob(FireInstanceIdRequest request)
        {
            var job = (await Scheduler.GetCurrentlyExecutingJobs()).FirstOrDefault(j => j.FireInstanceId == request.FireInstanceId);
            RunningJobDetails details = null;
            if (job != null)
            {
                details = new RunningJobDetails();
                MapJobRowDetails(job.JobDetail, details);
                MapJobExecutionContext(job, details);
            }

            return new BaseResponse<RunningJobDetails>(details);
        }

        public static async Task InvokeJob(InvokeJobRequest request)
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

        public static async Task<BaseResponse> PauseAll()
        {
            await Scheduler.PauseAll();
            return BaseResponse.Empty;
        }

        public static async Task PauseJob(JobOrTriggerKey request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            await Scheduler.PauseJob(jobKey);
        }

        public async Task<BaseResponse> RemoveGlobalParameter(GlobalParameterKey request)
        {
            await _dal.RemoveGlobalParameter(request.Key);
            return BaseResponse.Empty;
        }

        public static async Task<bool> RemoveJob(JobOrTriggerKey request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            ValidateSystemJob(jobKey);
            var result = await Scheduler.DeleteJob(jobKey);
            return result;
        }

        private static void ValidateSystemJob(JobKey jobKey)
        {
            if (jobKey.Group == Consts.PlannerSystemGroup)
            {
                throw new PlannerValidationException($"Forbidden: this is system job and it should not be modified or deleted");
            }
        }

        private static void ValidateSystemTrigger(TriggerKey triggerKey)
        {
            if (triggerKey.Group == Consts.PlannerSystemGroup)
            {
                throw new PlannerValidationException($"Forbidden: this is system trigger and it should not be modified or deleted");
            }
        }

        public static async Task<BaseResponse> RemoveJobData(RemoveJobDataRequest request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            ValidateSystemJob(jobKey);
            var info = await Scheduler.GetJobDetail(jobKey);
            await ValidateJobNotRunning(jobKey);
            await Scheduler.PauseJob(jobKey);

            if (info != null && info.JobDataMap.ContainsKey(request.DataKey))
            {
                info.JobDataMap.Remove(request.DataKey);
            }
            else
            {
                throw new PlannerValidationException($"Data with Key '{request.DataKey}' could not found in job '{request.Id}' (Name '{jobKey.Name}' and Group '{jobKey.Group}')");
            }

            var triggers = await Scheduler.GetTriggersOfJob(jobKey);
            await Scheduler.ScheduleJob(info, triggers, true);
            await Scheduler.ResumeJob(jobKey);

            return BaseResponse.Empty;
        }

        public static async Task<BaseResponse> ResumeAll()
        {
            await Scheduler.ResumeAll();
            return BaseResponse.Empty;
        }

        public static async Task ResumeJob(JobOrTriggerKey request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            await Scheduler.ResumeJob(jobKey);
        }

        public static async Task<BaseResponse> UpsertJobProperty(UpsertJobPropertyRequest request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            ValidateSystemJob(jobKey);
            await ValidateJobNotRunning(jobKey);
            await Scheduler.PauseJob(jobKey);
            var info = await Scheduler.GetJobDetail(jobKey);
            var propertiesJson = Convert.ToString(info.JobDataMap[Consts.JobTypeProperties]);
            Dictionary<string, string> properties;
            if (string.IsNullOrEmpty(propertiesJson))
            {
                properties = new Dictionary<string, string>();
            }
            else
            {
                try
                {
                    properties = JsonConvert.DeserializeObject<Dictionary<string, string>>(propertiesJson);
                }
                catch
                {
                    properties = new Dictionary<string, string>();
                }
            }

            if (properties.ContainsKey(request.PropertyKey))
            {
                properties[request.PropertyKey] = request.PropertyValue;
            }
            else
            {
                properties.Add(request.PropertyKey, request.PropertyValue);
            }

            propertiesJson = JsonConvert.SerializeObject(properties);

            if (info.JobDataMap.ContainsKey(Consts.JobTypeProperties))
            {
                info.JobDataMap.Put(Consts.JobTypeProperties, propertiesJson);
            }
            else
            {
                info.JobDataMap.Add(Consts.JobTypeProperties, propertiesJson);
            }

            var triggers = await Scheduler.GetTriggersOfJob(jobKey);
            await Scheduler.ScheduleJob(info, triggers, true);
            await Scheduler.ResumeJob(jobKey);

            return BaseResponse.Empty;
        }

        public static async Task StopScheduler(StopSchedulerRequest request)
        {
            await Scheduler.Shutdown(request.WaitJobsToComplete);

            var t = Task.Run(async () =>
            {
                await Task.Delay(3000);
                MainService.Shutdown();
            });
        }

        public static async Task<BaseResponse> UpsertJobData(JobDataRequest request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            ValidateSystemJob(jobKey);
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

            return BaseResponse.Empty;
        }

        public async Task<BaseResponse> UpsertGlobalParameter(GlobalParameterData request)
        {
            var exists = await _dal.IsGlobalParameterExists(request.Key);
            var data = GetGlobalParameter(request);
            if (exists)
            {
                await _dal.UpdateGlobalParameter(data);
            }
            else
            {
                await _dal.AddGlobalParameter(data);
            }

            return BaseResponse.Empty;
        }

        public static async Task<BaseResponse> RemoveTrigger(JobOrTriggerKey request)
        {
            var triggerKey = await TriggerKeyHelper.GetTriggerKey(request);
            ValidateSystemTrigger(triggerKey);
            await Scheduler.PauseTrigger(triggerKey);
            var success = await Scheduler.UnscheduleJob(triggerKey);
            if (success == false)
            {
                throw new ApplicationException("Fail to remove trigger");
            }

            return BaseResponse.Empty;
        }

        public static async Task<BaseResponse> PauseTrigger(JobOrTriggerKey request)
        {
            var key = await TriggerKeyHelper.GetTriggerKey(request);
            await Scheduler.PauseTrigger(key);
            return BaseResponse.Empty;
        }

        public static async Task<BaseResponse> ResumeTrigger(JobOrTriggerKey request)
        {
            var key = await TriggerKeyHelper.GetTriggerKey(request);
            await Scheduler.ResumeTrigger(key);
            return BaseResponse.Empty;
        }

        public static async Task<BaseResponse> AddTrigger(AddTriggerRequest request)
        {
            var metadata = GetJobMetadata(request.Yaml);
            ValidateTriggerMetadata(metadata);
            var key = await JobKeyHelper.GetJobKey(request);
            var job = await Scheduler.GetJobDetail(key);
            await BuildTriggers(Scheduler, job, metadata);
            return BaseResponse.Empty;
        }

        public async Task<BaseResponse<LastInstanceId>> GetLastInstanceId(GetLastInstanceIdRequest request)
        {
            var jobKey = await JobKeyHelper.GetJobKey(request);
            var result = await _dal.GetLastInstanceId(jobKey, request.InvokeDate);
            return new BaseResponse<LastInstanceId>(result);
        }

        public async Task<HistoryCallForJobResponse> GetHistory(GetHistoryRequest request)
        {
            if (request.Rows.GetValueOrDefault() == 0) { request.Rows = 50; }
            var result = await _dal.GetHistory(request);
            return new HistoryCallForJobResponse(result);
        }

        public async Task<BaseResponse<JobInstanceLog>> GetHistoryById(GetByIdRequest request)
        {
            var result = await _dal.GetHistoryById(request.Id);
            var response = JsonMapper.Map<JobInstanceLog, DbJobInstanceLog>(result);
            return new BaseResponse<JobInstanceLog>(response);
        }

        public async Task<BaseResponse<GetTestStatusResponse>> GetTestStatus(GetByIdRequest request)
        {
            var result = await _dal.GetTestStatus(request.Id);
            return new BaseResponse<GetTestStatusResponse>(result);
        }

        public async Task<BaseResponse<HistoryFieldData>> GetHistoryDataById(GetByIdRequest request)
        {
            var result = await _dal.GetHistoryDataById(request.Id);
            var response = new HistoryFieldData { Data = result };
            return new BaseResponse<HistoryFieldData>(response);
        }

        public async Task<BaseResponse<HistoryFieldData>> GetHistoryInformationById(GetByIdRequest request)
        {
            var result = await _dal.GetHistoryInformationById(request.Id);
            var response = new HistoryFieldData { Data = result };
            return new BaseResponse<HistoryFieldData>(response);
        }

        public async Task<BaseResponse<HistoryFieldData>> GetHistoryExceptionById(GetByIdRequest request)
        {
            var result = await _dal.GetHistoryExceptionById(request.Id);
            var response = new HistoryFieldData { Data = result };
            return new BaseResponse<HistoryFieldData>(response);
        }

        public async Task<HistoryCallForJobResponse> GetLastHistoryCallForJob(GetLastHistoryCallForJobRequest request)
        {
            var result = (await _dal.GetLastHistoryCallForJob(request)).ToList();
            return new HistoryCallForJobResponse(result);
        }

        public async Task<BaseResponse<AddUserResponse>> AddUser(AddUserRequest request)
        {
            var password = PasswordGenerator.GeneratePassword(
                new PasswordGeneratorBuilder()
                .IncludeLowercase()
                .IncludeNumeric()
                .IncludeSpecial()
                .IncludeUppercase()
                .WithLength(12)
                .Build());

            var user = new User
            {
                Username = request.Username,
                EmailAddress1 = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber1 = request.PhoneNumber,
                Password = password
            };

            var result = await _dal.AddUser(user);
            var response = new AddUserResponse
            {
                Id = result.Id,
                Password = result.Password
            };

            return new BaseResponse<AddUserResponse>(response);
        }

        public async Task<BaseResponse<string>> GetUser(GetByIdRequest request)
        {
            return await GetBaseResponse(() => _dal.GetUser(request.Id));
        }

        public async Task<BaseResponse<string>> GetUsers()
        {
            return await GetBaseResponse(_dal.GetUsers);
        }

        public async Task<BaseResponse> RemoveUser(GetByIdRequest request)
        {
            var user = new User { Id = request.Id };
            await _dal.RemoveUser(user);
            return BaseResponse.Empty;
        }

        public async Task<BaseResponse> UpdateUser(string request)
        {
            var entity = DeserializeObject<UpdateEntityRecord>(request);
            await new UpdateEntityRecordValidator().ValidateAndThrowAsync(entity);

            if ((await _dal.GetUser(entity.Id)) is not User existsUser)
            {
                throw new PlannerValidationException($"User with id {entity.Id} could not be found");
            }

            var properties = typeof(User).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var prop = properties.FirstOrDefault(p => p.Name == entity.PropertyName);
            if (prop == null)
            {
                throw new PlannerValidationException($"PropertyName '{entity.PropertyName}' could not be found in User entity");
            }

            try
            {
                var stringValue = entity.PropertyValue;
                if (stringValue.ToLower() == "[null]") { stringValue = null; }
                var value = Convert.ChangeType(stringValue, prop.PropertyType);
                prop.SetValue(existsUser, value);
            }
            catch (Exception ex)
            {
                throw new PlannerValidationException($"PropertyValue '{entity.PropertyValue}' could not be set to PropertyName '{entity.PropertyName}' ({ex.Message})");
            }

            await new UpdateUserValidator().ValidateAndThrowAsync(existsUser);

            await _dal.UpdateUser(existsUser);

            return BaseResponse.Empty;
        }

        public async Task<BaseResponse<string>> GetUserPassword(GetByIdRequest request)
        {
            var password = await _dal.GetPassword(request.Id);
            return new BaseResponse<string>(password);
        }

        public async Task<BaseResponse> AddGroup(string request)
        {
            var entity = DeserializeObject<UpsertGroupRecord>(request);

            var group = new Group
            {
                Id = entity.Id,
                Name = entity.Name
            };

            await _dal.AddGroup(group);
            return BaseResponse.Empty;
        }

        public async Task<BaseResponse<string>> GetGroupById(GetByIdRequest request)
        {
            return await GetBaseResponse(() => _dal.GetGroup(request.Id));
        }

        public async Task<BaseResponse<string>> GetGroups()
        {
            return await GetBaseResponse(_dal.GetGrousp);
        }

        public async Task<BaseResponse> RemoveGroup(GetByIdRequest request)
        {
            var group = new Group { Id = request.Id };
            return await GetBaseResponse(() => _dal.RemoveGroup(group));
        }

        public async Task<BaseResponse> UpdateGroup(string request)
        {
            var entity = DeserializeObject<UpdateEntityRecord>(request);
            await new UpdateEntityRecordValidator().ValidateAndThrowAsync(entity);

            if (await _dal.GetGroup(entity.Id) is not Group existsGroup)
            {
                throw new PlannerValidationException($"Group with id {entity.Id} could not be found");
            }

            var properties = typeof(Group).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var prop = properties.FirstOrDefault(p => p.Name == entity.PropertyName);
            if (prop == null)
            {
                throw new PlannerValidationException($"PropertyName '{entity.PropertyName}' could not be found in Group entity");
            }

            try
            {
                var stringValue = entity.PropertyValue;
                if (stringValue.ToLower() == "[null]") { stringValue = null; }
                var value = Convert.ChangeType(stringValue, prop.PropertyType);
                prop.SetValue(existsGroup, value);
            }
            catch (Exception ex)
            {
                throw new PlannerValidationException($"PropertyValue '{entity.PropertyValue}' could not be set to PropertyName '{entity.PropertyName}' ({ex.Message})");
            }

            await new GroupValidator().ValidateAndThrowAsync(existsGroup);

            await _dal.UpdateGroup(existsGroup);

            return BaseResponse.Empty;
        }

        public async Task<BaseResponse> AddUserToGroup(string request)
        {
            var entity = DeserializeObject<AddUserToGroupRecord>(request);

            if (await _dal.IsUserExists(entity.UserId) == false) { throw new PlannerValidationException($"UserId {entity.UserId} is not exists"); }
            if (await _dal.IsGroupExists(entity.GroupId) == false) { throw new PlannerValidationException($"GroupId {entity.GroupId} is not exists"); }
            if (await _dal.IsUserExistsInGroup(entity.UserId, entity.GroupId)) { throw new PlannerValidationException($"UserId {entity.UserId} already in GroupId {entity.GroupId}"); }

            await _dal.AddUserToGroup(entity.UserId, entity.GroupId);
            return BaseResponse.Empty;
        }

        public async Task<BaseResponse> RemoveUserFromGroup(string request)
        {
            var entity = DeserializeObject<AddUserToGroupRecord>(request);

            if (await _dal.IsUserExists(entity.UserId) == false) { throw new PlannerValidationException($"UserId {entity.UserId} is not exists"); }
            if (await _dal.IsGroupExists(entity.GroupId) == false) { throw new PlannerValidationException($"GroupId {entity.GroupId} is not exists"); }
            if (await _dal.IsUserExistsInGroup(entity.UserId, entity.GroupId) == false) { throw new PlannerValidationException($"UserId {entity.UserId} is not exists in GroupId {entity.GroupId}"); }

            await _dal.RemoveUserFromGroup(entity.UserId, entity.GroupId);
            return BaseResponse.Empty;
        }

        public BaseResponse<string> ReloadMonitor()
        {
            var sb = new StringBuilder();

            ServiceUtil.LoadMonitorHooks(_logger);
            sb.AppendLine($"{ServiceUtil.MonitorHooks.Count} monitor hooks loaded");
            MonitorUtil.Load();
            sb.AppendLine($"{MonitorUtil.Count} monitor items loaded");
            MonitorUtil.Validate(_logger);

            return new BaseResponse<string>(sb.ToString());
        }

        public static BaseResponse<List<string>> GetMonitorHooks()
        {
            return new BaseResponse<List<string>>(ServiceUtil.MonitorHooks.Keys.ToList());
        }

        public async Task<BaseResponse<List<MonitorItem>>> GetMonitorItems(GetMonitorItemsRequest request)
        {
            var items = await _dal.GetMonitorItems(request);
            var result = items.Select(m => new MonitorItem
            {
                Active = m.Active.GetValueOrDefault(),
                EventTitle = ((MonitorEvents)m.EventId).ToString(),
                GroupName = m.Group.Name,
                Hook = m.Hook,
                Id = m.Id,
                Job = string.IsNullOrEmpty(m.JobGroup) ? $"Id: {m.JobId}" : $"Group: {m.JobGroup}",
                Title = m.Title
            })
            .ToList();

            return new BaseResponse<List<MonitorItem>>(result);
        }

        public async Task<BaseResponse<MonitorActionMedatada>> GetMonitorActionMedatada()
        {
            var result = new MonitorActionMedatada();

            result.Hooks = ServiceUtil.MonitorHooks.Keys
                .Select((k, i) => new { k, i })
                .ToDictionary(i => i.i, k => k.k);

            var response = new BaseResponse<MonitorActionMedatada>(result);
            return await Task.FromResult(response);
        }

        #region Private

        private static GlobalParameter GetGlobalParameter(GlobalParameterData request)
        {
            var result = new GlobalParameter
            {
                ParamKey = request.Key,
                ParamValue = request.Value
            };

            return result;
        }

        private static GlobalParameterData GetGlobalParameterData(GlobalParameter data)
        {
            var result = new GlobalParameterData
            {
                Key = data.ParamKey,
                Value = data.ParamValue
            };

            return result;
        }

        private static async Task<BaseResponse<TriggerRowDetails>> GetTriggersDetails(JobKey jobKey)
        {
            var result = new BaseResponse<TriggerRowDetails>(new TriggerRowDetails());
            var triggers = await Scheduler.GetTriggersOfJob(jobKey);

            foreach (var t in triggers)
            {
                if (t is ISimpleTrigger t1)
                {
                    result.Result.SimpleTriggers.Add(MapSimpleTriggerDetails(t1));
                }
                else
                {
                    if (t is ICronTrigger t2)
                    {
                        result.Result.CronTriggers.Add(MapCronTriggerDetails(t2));
                    }
                }
            }

            return result;
        }

        private static async Task<BaseResponse<TriggerRowDetails>> GetTriggerDetails(TriggerKey triggerKey)
        {
            var result = new BaseResponse<TriggerRowDetails>(new TriggerRowDetails());
            var trigger = await Scheduler.GetTrigger(triggerKey);

            if (trigger is ISimpleTrigger t1)
            {
                result.Result.SimpleTriggers.Add(MapSimpleTriggerDetails(t1));
            }
            else
            {
                if (trigger is ICronTrigger t2)
                {
                    result.Result.CronTriggers.Add(MapCronTriggerDetails(t2));
                }
            }

            return result;
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

            MapJobRowDetails(source, target);
            target.ConcurrentExecution = !source.ConcurrentExecutionDisallowed;
            target.Durable = source.Durable;
            target.RequestsRecovery = source.RequestsRecovery;
            target.DataMap = ServiceUtil.ConvertJobDataMapToDictionary(dataMap);

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

        private static void MapJobExecutionContext(IJobExecutionContext source, RunningJobDetails target)
        {
            target.FireInstanceId = source.FireInstanceId;
            target.NextFireTime = source.NextFireTimeUtc.HasValue ? source.NextFireTimeUtc.Value.DateTime : (DateTime?)null;
            target.PreviousFireTime = source.PreviousFireTimeUtc.HasValue ? source.PreviousFireTimeUtc.Value.DateTime : (DateTime?)null;
            target.ScheduledFireTime = source.ScheduledFireTimeUtc.HasValue ? source.ScheduledFireTimeUtc.Value.DateTime : (DateTime?)null;
            target.FireTime = source.FireTimeUtc.DateTime;
            target.RunTime = source.JobRunTime;
            target.RefireCount = source.RefireCount;
            target.TriggerGroup = source.Trigger.Key.Group;
            target.TriggerName = source.Trigger.Key.Name;
            target.DataMap = ServiceUtil.ConvertJobDataMapToDictionary(source.MergedJobDataMap);
            target.TriggerId = Convert.ToString(Convert.ToString(source.Get(Consts.TriggerId)));

            var metadata = JobExecutionMetadataUtil.GetInstance(source);
            target.EffectedRows = metadata.EffectedRows;
            target.Progress = metadata.Progress;
        }

        private static void MapJobRowDetails(IJobDetail source, JobRowDetails target)
        {
            target.Id = Convert.ToString(source.JobDataMap[Consts.JobId]);
            target.Name = source.Key.Name;
            target.Group = source.Key.Group;
            target.Description = source.Description;
        }

        private static SimpleTriggerDetails MapSimpleTriggerDetails(ISimpleTrigger source)
        {
            var result = new SimpleTriggerDetails();
            MapTriggerDetails(source, result);
            result.RepeatCount = source.RepeatCount;
            result.RepeatInterval = source.RepeatInterval;
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
            target.DataMap = source.JobDataMap
                .AsEnumerable()
                .Where(s => s.Key.StartsWith(Consts.ConstPrefix) == false && s.Key.StartsWith(Consts.QuartzPrefix) == false)
                .ToDictionary(k => k.Key, v => Convert.ToString(v.Value));
            target.State = Scheduler.GetTriggerState(source.Key).Result.ToString();
            target.Id = Convert.ToString(source.JobDataMap[Consts.TriggerId]);

            if (source.Key.Group == Consts.RecoveringJobsGroup)
            {
                target.Id = string.Empty.PadLeft(11, '-');
            }
        }

        private static async Task ValidateJobNotExists(JobKey jobKey)
        {
            var exists = await Scheduler.GetJobDetail(jobKey);

            if (exists != null)
            {
                throw new PlannerValidationException($"job with name: {jobKey.Name} and group: {jobKey.Group} already exists");
            }
        }

        private static async Task ValidateJobNotRunning(JobKey jobKey)
        {
            var allRunning = await Scheduler.GetCurrentlyExecutingJobs();
            if (allRunning.AsQueryable().Any(c => c.JobDetail.Key.Name == jobKey.Name && c.JobDetail.Key.Group == jobKey.Group))
            {
                throw new PlannerValidationException($"job with name: {jobKey.Name} and group: {jobKey.Group} is currently running");
            }
        }

        #endregion Private
    }
}