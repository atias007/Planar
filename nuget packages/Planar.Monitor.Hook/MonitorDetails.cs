using System;
using System.Collections.Generic;

namespace Planar.Monitor.Hook
{
    internal class MonitorDetails : Monitor, IMonitorDetails
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
        public Dictionary<string, string> MergedJobDataMap { get; set; }
        public string FireInstanceId { get; set; }
        public DateTime FireTime { get; set; }
        public TimeSpan JobRunTime { get; set; }
        public bool Recovering { get; set; }
    }
}