using System;

namespace Planar.API.Common.Entities
{
    public class GetHistoryRequest : PagingRequest, IDateScope
    {
        public string? JobId { get; set; }

        public string? JobGroup { get; set; }

        public string? JobType { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public bool Ascending { get; set; }

        public StatusMembers? Status { get; set; }
    }
}