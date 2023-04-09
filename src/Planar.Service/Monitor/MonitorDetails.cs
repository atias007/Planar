using System;
using System.Collections.Generic;

namespace Planar.Service.Monitor
{
    public class MonitorDetails : Monitor
    {
        public string JobName { get; set; } = null!;
        public string JobGroup { get; set; } = null!;
        public string? JobId { get; set; } = null!; = null!;
        public string? JobDescription { get; set; }
        public bool Durable { get; set; }
        public string TriggerName { get; set; } = null!;
        public string TriggerGroup { get; set; } = null!;
        public string? TriggerId { get; set; } = null!;
        public string? TriggerDescription { get; set; }
        public string? Calendar { get; set; }
        public string? Author { get; set; }
        public SortedDictionary<string, string?> MergedJobDataMap { get; set; } = new();
        public string FireInstanceId { get; set; } = null!;
        public DateTime FireTime { get; set; }
        public TimeSpan JobRunTime { get; set; }
        public bool Recovering { get; set; }
    }
}