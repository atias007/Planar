using System.ComponentModel.DataAnnotations.Schema;

namespace EFCoreTest
{
    [Table("JobInstanceLog")]
    internal class JobInstanceLog
    {
        public long Id { get; set; }

        public string InstanceId { get; set; }

        public string JobId { get; set; }

        public string JobName { get; set; }

        public string JobGroup { get; set; }
        public string TriggerId { get; set; }
        public string TriggerName { get; set; }
        public string TriggerGroup { get; set; }
        public string ServerName { get; set; }
        public int Status { get; set; }
        public string StatusTitle { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Duration { get; set; }
        public int? EffectedRows { get; set; }
        public string Data { get; set; }
        public string Log { get; set; }
        public string Exception { get; set; }
        public bool Retry { get; set; }
        public bool IsStopped { get; set; }
        public byte? Anomaly { get; set; }
    }
}