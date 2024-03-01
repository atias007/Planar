using System;

namespace Planar.Client.Entities
{
    public class MuteDetails
    {
        public string? JobId { get; set; }
        public int? MonitorId { get; set; }
        public string? MonitorTitle { get; set; }
        public DateTime DueDate { get; set; }
    }
}