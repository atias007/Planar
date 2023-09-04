namespace Planar.Client.Entities
{
    public class MonitorItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
        public string? JobName { get; set; }
        public string? JobGroup { get; set; }
        public string DistributionGroupName { get; set; } = string.Empty;
        public string? EventArgument { get; set; }
        public string Hook { get; set; } = string.Empty;
        public bool Active { get; set; }
    }
}