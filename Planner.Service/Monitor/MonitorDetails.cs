using Planner.MonitorHook;
using System;
using System.Collections.Generic;

namespace Planner.Service.Monitor
{
    public class MonitorDetails : IMonitorDetails
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
        public string MonitorTitle { get; set; }
        public int EventId { get; set; }
        public IMonitorGroup Group { get; set; }
        public List<IMonitorUser> Users { get; set; } = new();
        public string EventTitle { get; set; }
        public Exception Exception { get; set; }
    }
}