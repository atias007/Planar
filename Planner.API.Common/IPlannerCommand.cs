using Planner.API.Common.Entities;
using System.Collections.Generic;

namespace Planner.API.Common
{
    public interface IPlannerCommand
    {
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

        #region User

        BaseResponse<AddUserResponse> AddUser(AddUserRequest request);

        BaseResponse<string> GetUser(GetByIdRequest request);

        BaseResponse<string> GetUsers();

        BaseResponse RemoveUser(GetByIdRequest request);

        BaseResponse UpdateUser(string request);

        BaseResponse<string> GetUserPassword(GetByIdRequest request);

        #endregion User

        #region Monitor

        BaseResponse<string> ReloadMonitor();

        BaseResponse<List<string>> GetMonitorHooks();

        BaseResponse<List<MonitorItem>> GetMonitorActions(GetMonitorActionsRequest request);

        BaseResponse<MonitorActionMedatada> GetMonitorActionMedatada();

        BaseResponse<List<string>> GetMonitorEvents();

        BaseResponse AddMonitor(AddMonitorRequest request);

        BaseResponse DeleteMonitor(GetByIdRequest request);

        #endregion Monitor
    }
}