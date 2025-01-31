using System;

namespace Planar.Client.Entities
{
    public class MonitorAlertDetails
    {
        public int Id { get; set; }

        public int MonitorId { get; set; }

#if NETSTANDARD2_0
        public string MonitorTitle { get; set; }
        public string EventTitle { get; set; }
        public string EventArgument { get; set; }
        public string JobName { get; set; }
        public string JobGroup { get; set; }
        public string JobId { get; set; }
        public string GroupName { get; set; }
        public string Hook { get; set; }
        public string LogInstanceId { get; set; }
        public string Exception { get; set; }
        public string AlertPayload { get; set; }
#else
        public string MonitorTitle { get; set; } = null!;

        public string EventTitle { get; set; } = null!;

        public string? EventArgument { get; set; }

        public string? JobName { get; set; }

        public string? JobGroup { get; set; }

        public string? JobId { get; set; }

        public string GroupName { get; set; } = null!;

        public string Hook { get; set; } = null!;

        public string? LogInstanceId { get; set; }

        public string? Exception { get; set; }

        public string? AlertPayload { get; set; }
#endif

        public bool HasError { get; set; }

        public DateTime AlertDate { get; set; }
        public int UsersCount { get; set; }
    }
}