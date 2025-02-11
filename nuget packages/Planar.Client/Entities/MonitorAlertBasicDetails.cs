using System;

namespace Planar.Client.Entities
{
    public class MonitorAlertBasicDetails
    {
        public int Id { get; set; }

#if NETSTANDARD2_0
        public string MonitorTitle { get; set; }
        public string EventTitle { get; set; }
        public string EventArgument { get; set; }
        public string JobId { get; set; }
        public string JobName { get; set; }
        public string JobGroup { get; set; }
        public string GroupName { get; set; }
        public string Hook { get; set; }
#else
        public string MonitorTitle { get; set; } = null!;

        public string EventTitle { get; set; } = null!;

        public string? EventArgument { get; set; }

        public string? JobId { get; set; }

        public string? JobName { get; set; }

        public string? JobGroup { get; set; }

        public string GroupName { get; set; } = null!;

        public string Hook { get; set; } = null!;
#endif

        public DateTime AlertDate { get; set; }

        public bool HasError { get; set; }
    }
}