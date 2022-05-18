using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class SimpleTriggerDetails : TriggerDetails
    {
        [YamlMember(Order = 97)]
        public int RepeatCount { get; set; }

        [YamlMember(Order = 98)]
        public string RepeatInterval { get; set; }

        [YamlMember(Order = 96)]
        public int TimesTriggered { get; set; }
    }
}