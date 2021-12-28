using System.Collections.Generic;

namespace Planner.API.Common.Entities
{
    public class GetAllJobsResponse : BaseResponse<List<JobRowDetails>>
    {
        public GetAllJobsResponse()
            : base(null)
        {
        }

        public GetAllJobsResponse(List<JobRowDetails> tasksDetails)
           : base(tasksDetails)
        {
        }
    }
}