using System;
using System.Collections.Generic;

namespace Planar.Job
{
    public interface ITriggerDetail
    {
        string CalendarName { get; set; }
        string Description { get; set; }
        DateTimeOffset? EndTime { get; set; }
        DateTimeOffset? FinalFireTime { get; set; }
        bool HasMillisecondPrecision { get; set; }
        IKey Key { get; set; }
        int MisfireInstruction { get; set; }
        int Priority { get; set; }
        DateTimeOffset StartTime { get; set; }
        SortedDictionary<string, string> TriggerDataMap { get; set; }
    }
}