using System;
using System.Collections.Generic;

namespace Planar.Service.Monitor
{
    public class MonitorDetails : Monitor
    {
        public string JobName { get; set; }
        public string JobGroup { get; set; }
        public string JobId { get; set; }
        public string JobDescription { get; set; }
        public bool Durable { get; set; }
        public string TriggerName { get; set; }
        public string TriggerGroup { get; set; }
        public string TriggerId { get; set; }
        public string TriggerDescription { get; set; }
        public string Calendar { get; set; }
        public SortedDictionary<string, string> MergedJobDataMap { get; set; } = new();
        public string FireInstanceId { get; set; }
        public DateTime FireTime { get; set; }
        public TimeSpan JobRunTime { get; set; }
        public bool Recovering { get; set; }
    }
}