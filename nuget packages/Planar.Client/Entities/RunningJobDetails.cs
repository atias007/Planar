namespace Planar.Client.Entities
{
    public class RunningJobDetails : JobBasicDetails
    {
        public string FireInstanceId { get; set; } = string.Empty;

        public DateTime? ScheduledFireTime { get; set; }

        public DateTime FireTime { get; set; }

        public DateTime? NextFireTime { get; set; }

        public DateTime? PreviousFireTime { get; set; }

        public TimeSpan RunTime { get; set; }

        public int RefireCount { get; set; }

        public string TriggerName { get; set; } = string.Empty;

        public string TriggerGroup { get; set; } = string.Empty;

        public string TriggerId { get; set; } = string.Empty;

        public int ExceptionsCount { get; set; }

        public SortedDictionary<string, string?> DataMap { get; set; } = new SortedDictionary<string, string?>();

        public int? EffectedRows { get; set; }

        public int Progress { get; set; }

        public TimeSpan? EstimatedEndTime { get; set; }
    }
}