namespace Planar.API.Common.Entities
{
    public class MonitorTestRequest
    {
        public string Hook { get; set; } = null!;

        public string EventName { get; set; } = null!;

        public string GroupName { get; set; } = null!;

        public int? EffectedRows { get; set; }
    }
}