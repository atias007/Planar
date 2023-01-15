using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class TriggerDetails : PausedTriggerDetails
    {
        [YamlMember(Order = 5)]
        public DateTime Start { get; set; }

        [YamlMember(Order = 6)]
        public DateTime? End { get; set; }

        [YamlMember(Order = 7)]
        public string CalendarName { get; set; }

        [YamlMember(Order = 8)]
        public TimeSpan? RetrySpan { get; set; }

        [YamlMember(Order = 9)]
        public string MisfireBehaviour { get; set; }

        [YamlMember(Order = 10)]
        public int Priority { get; set; }

        [YamlMember(Order = 11)]
        public DateTime? NextFireTime { get; set; }

        [YamlMember(Order = 12)]
        public DateTime? PreviousFireTime { get; set; }

        [YamlMember(Order = 13)]
        public bool MayFireAgain { get; set; }

        [YamlMember(Order = 14)]
        public DateTime? FinalFire { get; set; }

        [YamlMember(Order = 99)]
        public SortedDictionary<string, string> DataMap { get; set; }

        [YamlMember(Order = 100)]
        public string State { get; set; }
    }
}