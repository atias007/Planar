using Planner.API.Common.Entities;
using System.Collections.Generic;

namespace Planner.API.Common
{
    public interface IPlannerCommand
    {
        #region Service

        GetServiceInfoResponse GetServiceInfo();

        BaseResponse StopScheduler(StopSchedulerRequest request);

        BaseResponse<List<string>> GetAllCalendars();

        #endregion Service

        #region Job

        BaseResponse InvokeJob(InvokeJobRequest request);

        AddJobResponse AddJob(AddJobRequest request);

        BaseResponse PauseJob(JobOrTriggerKey request);

        BaseResponse ResumeJob(JobOrTriggerKey request);

        BaseResponse<bool> RemoveJob(JobOrTriggerKey request);

        BaseResponse<JobDetails> GetJobDetails(JobOrTriggerKey request);

        BaseResponse<Dictionary<string, string>> GetJobSettings(JobOrTriggerKey request);

        GetAllJobsResponse GetAllJobs();

        GetRunningJobsResponse GetRunningJobs(FireInstanceIdRequest request);

        BaseResponse StopRunningJob(FireInstanceIdRequest request);

        BaseResponse UpsertJobData(JobDataRequest request);

        BaseResponse RemoveJobData(RemoveJobDataRequest request);

        BaseResponse ClearJobData(JobOrTriggerKey request);

        BaseResponse PauseAll();

        BaseResponse ResumeAll();

        BaseResponse<RunningJobDetails> GetRunningJob(FireInstanceIdRequest request);

        BaseResponse<string> GetRunningInfo(FireInstanceIdRequest request);

        BaseResponse<GetTestStatusResponse> GetTestStatus(GetByIdRequest request);

        BaseResponse UpsertJobProperty(UpsertJobPropertyRequest request);

        #endregion Job

        #region Trigger

        BaseResponse<TriggerRowDetails> GetTriggersDetails(JobOrTriggerKey request);

        BaseResponse RemoveTrigger(JobOrTriggerKey request);

        BaseResponse AddTrigger(AddTriggerRequest request);

        BaseResponse PauseTrigger(JobOrTriggerKey request);

        BaseResponse ResumeTrigger(JobOrTriggerKey request);

        BaseResponse<TriggerRowDetails> GetTriggerDetails(JobOrTriggerKey request);

        #endregion Trigger

        #region Trace

        GetTraceResponse GetTrace(GetTraceRequest request);

        BaseResponse<string> GetTraceException(GetByIdRequest request);

        BaseResponse<string> GetTraceProperties(GetByIdRequest request);

        #endregion Trace

        #region Parameters

        BaseResponse UpsertGlobalParameter(GlobalParameterData request);

        BaseResponse RemoveGlobalParameter(GlobalParameterKey request);

        BaseResponse<string> GetGlobalParameter(GlobalParameterKey request);

        GetAllGlobalParametersResponse GetAllGlobalParameters();

        BaseResponse FlushGlobalParameter();

        #endregion Parameters

        #region History

        public BaseResponse<LastInstanceId> GetLastInstanceId(GetLastInstanceIdRequest request);

        public HistoryCallForJobResponse GetHistory(GetHistoryRequest request);

        public HistoryCallForJobResponse GetLastHistoryCallForJob(GetLastHistoryCallForJobRequest request);

        public BaseResponse<JobInstanceLog> GetHistoryById(GetByIdRequest request);

        public BaseResponse<HistoryFieldData> GetHistoryDataById(GetByIdRequest request);

        public BaseResponse<HistoryFieldData> GetHistoryInformationById(GetByIdRequest request);

        public BaseResponse<HistoryFieldData> GetHistoryExceptionById(GetByIdRequest request);

        #endregion History

        #region User

        BaseResponse<AddUserResponse> AddUser(AddUserRequest request);

        BaseResponse<string> GetUser(GetByIdRequest request);

        BaseResponse<string> GetUsers();

        BaseResponse RemoveUser(GetByIdRequest request);

        BaseResponse UpdateUser(string request);

        BaseResponse<string> GetUserPassword(GetByIdRequest request);

        #endregion User

        #region Group

        BaseResponse AddGroup(string request);

        BaseResponse<string> GetGroupById(GetByIdRequest request);

        BaseResponse<string> GetGroups();

        BaseResponse RemoveGroup(GetByIdRequest request);

        BaseResponse UpdateGroup(string request);

        BaseResponse AddUserToGroup(string request);

        BaseResponse RemoveUserFromGroup(string request);

        #endregion Group

        #region Monitor

        BaseResponse<string> ReloadMonitor();

        BaseResponse<List<string>> GetMonitorHooks();

        #endregion Monitor
    }
}