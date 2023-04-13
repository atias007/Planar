using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class RunningJobDetails : JobRowDetails
    {
        [YamlMember(Order = 20)]
        public string FireInstanceId { get; set; } = string.Empty;

        [YamlMember(Order = 21)]
        public DateTime? ScheduledFireTime { get; set; }

        [YamlMember(Order = 22)]
        public DateTime FireTime { get; set; }

        [YamlMember(Order = 23)]
        public DateTime? NextFireTime { get; set; }

        [YamlMember(Order = 24)]
        public DateTime? PreviousFireTime { get; set; }

        [YamlMember(Order = 25)]
        public TimeSpan RunTime { get; set; }

        [YamlMember(Order = 26)]
        public int RefireCount { get; set; }

        [YamlMember(Order = 27)]
        public string TriggerName { get; set; } = string.Empty;

        [YamlMember(Order = 28)]
        public string TriggerGroup { get; set; } = string.Empty;

        [YamlMember(Order = 29)]
        public string TriggerId { get; set; } = string.Empty;

        [YamlMember(Order = 30)]
        public SortedDictionary<string, string?> DataMap { get; set; } = new SortedDictionary<string, string?>();

        [YamlMember(Order = 998)]
        public int? EffectedRows { get; set; }

        [YamlMember(Order = 999)]
        public int Progress { get; set; }

        [YamlMember(Order = 1000)]
        public TimeSpan? EstimatedEndTime { get; set; }
    }
}