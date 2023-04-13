namespace Planar.Service.Model.DataObjects
{
    internal class JobInstanceLogForStatistics : IJobInstanceLogForStatistics
    {
        public long Id { get; set; }
        public string JobId { get; set; } = null!;
        public int Status { get; set; }
        public int? Duration { get; set; }
        public int? EffectedRows { get; set; }
        public bool IsStopped { get; set; }
        public byte? Anomaly { get; set; }
    }
}