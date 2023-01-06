using System;

namespace Planar.API.Common.Entities
{
    public class GetHistoryRequest
    {
        public string JobId { get; set; }

        public string JobGroup { get; set; }

        public int? Rows { get; set; }

        public DateTime? FromDate { get; set; }

        public bool Ascending { get; set; }

        public DateTime? ToDate { get; set; }

        public StatusMembers? Status { get; set; }
    }
}