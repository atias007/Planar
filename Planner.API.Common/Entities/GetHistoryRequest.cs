using System;

namespace Planner.API.Common.Entities
{
    public class GetHistoryRequest
    {
        public int? Rows { get; set; }

        public DateTime? FromDate { get; set; }

        public bool Ascending { get; set; }

        public DateTime? ToDate { get; set; }

        public StatusMembers? Status { get; set; }
    }
}