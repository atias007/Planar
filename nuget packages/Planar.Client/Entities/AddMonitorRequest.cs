namespace Planar.Client.Entities
{
    public class AddMonitorRequest
    {
        public string Title { get; set; } = null!;

        public string? JobName { get; set; }

        public string? JobGroup { get; set; }

        public string EventName { get; set; } = null!;

        public string? EventArgument { get; set; }

        public string GroupName { get; set; } = null!;

        public string Hook { get; set; } = null!;
    }
}