namespace Planar.API.Common.Entities
{
    public class AddMonitorRequest
    {
        public string Title { get; set; } = string.Empty;

        public string? JobName { get; set; }

        public string? JobGroup { get; set; }

        public MonitorEvents? EventId { get; set; }

        public string? EventArgument { get; set; }

        public int GroupId { get; set; }

        public string? Hook { get; set; }
    }
}