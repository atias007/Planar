using System;

namespace Planar.Client.Entities
{
    public class ListAlertsFilter : Paging, IDateScope
    {
#if NETSTANDARD2_0
        public string EventTitle { get; set; }
        public string JobGroup { get; set; }
        public string JobId { get; set; }
        public string GroupName { get; set; }
        public string Hook { get; set; }
#else
        public string? EventTitle { get; set; }
        public string? JobGroup { get; set; }
        public string? JobId { get; set; }
        public string? GroupName { get; set; }
        public string? Hook { get; set; }
#endif

        public bool? HasError { get; set; }
        public int? MonitorId { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? Ascending { get; set; }
    }
}