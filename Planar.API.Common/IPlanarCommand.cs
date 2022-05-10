using Planar.API.Common.Entities;
using System.Collections.Generic;

namespace Planar.API.Common
{
    public interface IPlanarCommand
    {
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

        #region User

        BaseResponse<AddUserResponse> AddUser(AddUserRequest request);

        BaseResponse<string> GetUser(GetByIdRequest request);

        BaseResponse<string> GetUsers();

        BaseResponse RemoveUser(GetByIdRequest request);

        BaseResponse UpdateUser(string request);

        BaseResponse<string> GetUserPassword(GetByIdRequest request);

        #endregion User
    }
}