namespace Planar.API.Common.Entities
{
    public class MonitorTestRequest
    {
        public string Hook { get; set; } = string.Empty;

        public TestMonitorEvents MonitorEvent { get; set; }

        public int DistributionGroupId { get; set; }

        public int? EffectedRows { get; set; }
    }
}