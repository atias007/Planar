namespace Planar.API.Common.Entities
{
    public class MonitorAlertRowModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string EventName { get; set; } = null!;

        public string? EventArgument { get; set; }

        public string? JobId { get; set; }

        public string? JobName { get; set; }

        public string? JobGroup { get; set; }

        public string GroupName { get; set; } = null!;

        public string Hook { get; set; } = null!;

        public bool HasError { get; set; }
    }
}