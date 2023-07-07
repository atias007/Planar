using System.Collections.Generic;

namespace Planar.API.Common.Entities
{
    public class HistoryJobResponse : PagingResponse<List<JobHistory>>
    {
        public HistoryJobResponse()
        {
        }

        public HistoryJobResponse(List<JobHistory> data, IPagingRequest request) : base(data, request)
        {
        }
    }
}