using System;
using System.Collections.Generic;

namespace Planar.MonitorHook
{
    public interface IMonitorDetails
    {
        string Calendar { get; set; }
        bool Durable { get; set; }
        int EventId { get; set; }
        string EventTitle { get; set; }
        string FireInstanceId { get; set; }
        DateTime FireTime { get; set; }
        string JobDescription { get; set; }
        string JobGroup { get; set; }
        string JobId { get; set; }
        string JobName { get; set; }
        TimeSpan JobRunTime { get; set; }
        SortedDictionary<string, string> MergedJobDataMap { get; set; }
        string MonitorTitle { get; set; }
        bool Recovering { get; set; }
        string TriggerDescription { get; set; }
        string TriggerGroup { get; set; }
        string TriggerId { get; set; }
        string TriggerName { get; set; }
        Exception Exception { get; set; }
        IMonitorGroup Group { get; set; }
        List<IMonitorUser> Users { get; set; }
    }
}