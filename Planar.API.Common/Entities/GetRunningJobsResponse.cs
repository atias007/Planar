﻿using System.Collections.Generic;

namespace Planar.API.Common.Entities
{
    public class GetRunningJobsResponse : BaseResponse<List<RunningJobDetails>>
    {
        public GetRunningJobsResponse()
            : base(null)
        {
        }

        public GetRunningJobsResponse(List<RunningJobDetails> result)
            : base(result)
        {
        }
    }
}