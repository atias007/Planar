using System.Collections.Generic;

namespace Planner.API.Common.Entities
{
    public class HistoryCallForJobResponse : BaseResponse<List<JobInstanceLogRow>>
    {
        public HistoryCallForJobResponse(List<JobInstanceLogRow> result) : base(result)
        {
        }
    }
}