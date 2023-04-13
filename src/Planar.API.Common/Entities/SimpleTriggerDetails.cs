using System;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class SimpleTriggerDetails : TriggerDetails
    {
        [YamlMember(Order = 40)]
        public int RepeatCount { get; set; }

        [YamlMember(Order = 41)]
        public TimeSpan RepeatInterval { get; set; }

        [YamlMember(Order = 42)]
        public int TimesTriggered { get; set; }
    }
}