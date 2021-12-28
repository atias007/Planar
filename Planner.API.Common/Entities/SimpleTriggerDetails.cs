using System;
using YamlDotNet.Serialization;

namespace Planner.API.Common.Entities
{
    public class SimpleTriggerDetails : TriggerDetails
    {
        [YamlMember(Order = 97)]
        public int RepeatCount { get; set; }

        [YamlMember(Order = 98)]
        public TimeSpan RepeatInterval { get; set; }

        [YamlMember(Order = 96)]
        public int TimesTriggered { get; set; }
    }
}