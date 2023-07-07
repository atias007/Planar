using System.Collections.Generic;

namespace Planar.API.Common.Entities
{
    public class HistoryJobRowResponse : PagingResponse<List<JobInstanceLogRow>>
    {
        public HistoryJobRowResponse()
        {
        }

        public HistoryJobRowResponse(List<JobInstanceLogRow> data, IPagingRequest request) : base(data, request)
        {
        }
    }
}