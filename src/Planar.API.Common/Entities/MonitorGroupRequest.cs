namespace Planar.API.Common.Entities
{
    public class MonitorGroupRequest
    {
        public string GroupName { get; set; } = null!;
        public int MonitorId { get; set; }
    }
}