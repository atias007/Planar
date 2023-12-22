using System;

namespace Planar.API.Common.Entities
{
    public class MuteItem
    {
        public string? JobId { get; set; }
        public int? MonitorId { get; set; }
        public string? MonitorTitle { get; set; }
        public DateTime DueDate { get; set; }
    }
}