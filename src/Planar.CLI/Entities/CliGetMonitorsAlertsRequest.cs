using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities
{
    public class CliGetMonitorsAlertsRequest : CliPagingRequest
    {
        [ActionProperty("tl", "title")]
        public string? EventTitle { get; set; }

        [ActionProperty("mi", "monitor-id")]
        public int? MonitorId { get; set; }

        [ActionProperty("j", "job")]
        public string? JobId { get; set; }

        [ActionProperty("g", "group")]
        public string? GroupName { get; set; }

        [ActionProperty("jg", "job-group")]
        public string? JobGroup { get; set; }

        [ActionProperty("h", "hook")]
        public string? Hook { get; set; }

        [ActionProperty("e", "errors")]
        public bool? HasError { get; set; }

        [ActionProperty("f", "from")]
        public DateTime FromDate { get; set; }

        [ActionProperty("a", "asc")]
        public bool? Ascending { get; set; }

        [ActionProperty("t", "to")]
        public DateTime ToDate { get; set; }
    }
}