using System;
using System.Collections.Generic;

namespace Planar.Monitor.Hook
{
    internal class MonitorDetails : Monitor, IMonitorDetails
    {
        public string JobName { get; set; } = string.Empty;
        public string JobGroup { get; set; } = string.Empty;
        public string JobId { get; set; } = string.Empty;
        public string? JobDescription { get; set; }
        public bool Durable { get; set; }
        public string? TriggerName { get; set; }
        public string? TriggerGroup { get; set; }
        public string TriggerId { get; set; } = string.Empty;
        public string? Calendar { get; set; }
        public string? Author { get; set; }
        public Dictionary<string, string> MergedJobDataMap { get; set; } = new Dictionary<string, string>();
        public string FireInstanceId { get; set; } = string.Empty;
        public DateTime FireTime { get; set; }
        public TimeSpan JobRunTime { get; set; }
        public bool Recovering { get; set; }
    }
}