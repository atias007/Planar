using System;

namespace Planar.API.Common.Entities
{
    public class GetSummaryRequest : PagingRequest
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}