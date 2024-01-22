using System;

namespace Planar.Client.Entities
{
    public class MonitorAlertBasicDetails
    {
        public int Id { get; set; }

        public string MonitorTitle { get; set; } = null!;

        public string EventTitle { get; set; } = null!;

        public string? EventArgument { get; set; }

        public DateTime AlertDate { get; set; }

        public string? JobId { get; set; }

        public string? JobName { get; set; }

        public string? JobGroup { get; set; }

        public string GroupName { get; set; } = null!;

        public string Hook { get; set; } = null!;

        public bool HasError { get; set; }
    }
}