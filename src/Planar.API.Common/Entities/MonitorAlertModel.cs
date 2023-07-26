using System;

namespace Planar.API.Common.Entities
{
    public partial class MonitorAlertModel
    {
        public int Id { get; set; }

        public int MonitorId { get; set; }

        public string MonitorTitle { get; set; } = null!;

        public string EventTitle { get; set; } = null!;

        public string? EventArgument { get; set; }

        public string? JobName { get; set; }

        public string? JobGroup { get; set; }

        public string? JobId { get; set; }

        public string GroupName { get; set; } = null!;

        public int UsersCount { get; set; }

        public string Hook { get; set; } = null!;

        public string? LogInstanceId { get; set; }

        public bool HasError { get; set; }

        public DateTime AlertDate { get; set; }

        public string? Exception { get; set; }

        public string? AlertPayload { get; set; }
    }
}