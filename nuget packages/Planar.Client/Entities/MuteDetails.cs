using System;

namespace Planar.Client.Entities
{
    public class MuteDetails
    {
#if NETSTANDARD2_0
        public string JobId { get; set; }
        public string MonitorTitle { get; set; }
#else
        public string? JobId { get; set; }
        public string? MonitorTitle { get; set; }
#endif

        public int? MonitorId { get; set; }

        public DateTime DueDate { get; set; }
    }
}