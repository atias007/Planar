using System;

namespace Planar.API.Common.Entities
{
    public class GetMonitorsAlertsRequest : PagingRequest
    {
        public string? EventTitle { get; set; }
        public int? MonitorId { get; set; }
        public string? JobGroup { get; set; }
        public string? JobId { get; set; }
        public string? GroupName { get; set; }
        public string? Hook { get; set; }
        public bool? HasError { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool Ascending { get; set; }
    }
}