using System;

namespace Planar.API.Common.Entities
{
    public class SecurityAuditsFilter : PagingRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool Ascending { get; set; }
    }
}