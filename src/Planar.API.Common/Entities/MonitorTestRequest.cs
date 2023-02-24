namespace Planar.API.Common.Entities
{
    public class MonitorTestRequest
    {
        public string Hook { get; set; }

        public TestMonitorEvents MonitorEvent { get; set; }

        public int DistributionGroupId { get; set; }

        public int? EffectedRows { get; set; }
    }
}