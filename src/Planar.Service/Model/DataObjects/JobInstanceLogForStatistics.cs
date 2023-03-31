namespace Planar.Service.Model.DataObjects
{
    internal class JobInstanceLogForStatistics
    {
        public int Id { get; set; }
        public string JobId { get; set; } = null!;
        public int Status { get; set; }
        public int? Duration { get; set; }
        public int? EffectedRows { get; set; }
        public bool IsStopped { get; set; }
        public bool? Anomaly { get; set; }
    }
}