using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class TriggerDetails
    {
        [YamlMember(Order = 0)]
        public string Id { get; set; }

        [YamlMember(Order = 1)]
        public string Name { get; set; }

        [YamlMember(Order = 2)]
        public string Group { get; set; }

        [YamlMember(Order = 3)]
        public string Description { get; set; }

        [YamlMember(Order = 4)]
        public DateTime Start { get; set; }

        [YamlMember(Order = 5)]
        public DateTime? End { get; set; }

        [YamlMember(Order = 6)]
        public string CalendarName { get; set; }

        [YamlMember(Order = 7)]
        public TimeSpan? RetrySpan { get; set; }

        [YamlMember(Order = 8)]
        public string MisfireBehaviour { get; set; }

        [YamlMember(Order = 9)]
        public int Priority { get; set; }

        [YamlMember(Order = 10)]
        public DateTime? NextFireTime { get; set; }

        [YamlMember(Order = 11)]
        public DateTime? PreviousFireTime { get; set; }

        [YamlMember(Order = 12)]
        public bool MayFireAgain { get; set; }

        [YamlMember(Order = 13)]
        public DateTime? FinalFire { get; set; }

        [YamlMember(Order = 99)]
        public SortedDictionary<string, string> DataMap { get; set; }

        [YamlMember(Order = 100)]
        public string State { get; set; }
    }
}