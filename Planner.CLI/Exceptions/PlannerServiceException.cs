using Planner.API.Common.Entities;
using System;

namespace Planner.CLI.Exceptions
{
    public class PlannerServiceException : Exception
    {
        public PlannerServiceException(Exception innerException)
            : base(string.Empty, innerException)
        {
        }

        public PlannerServiceException(BaseResponse response)
            : base($"({response.ErrorCode}) {response.ErrorDescription}")
        {
        }
    }
}