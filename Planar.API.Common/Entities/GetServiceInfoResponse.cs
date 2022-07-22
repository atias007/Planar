using System;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class GetServiceInfoResponse
    {
        [YamlMember(Order = 0)]
        public string Environment { get; set; }

        [YamlMember(Order = 1)]
        public bool IsStarted { get; set; }

        [YamlMember(Order = 2)]
        public bool InStandbyMode { get; set; }

        [YamlMember(Order = 3)]
        public bool IsShutdown { get; set; }

        [YamlMember(Order = 6)]
        public bool Clustered { get; set; }

        [YamlMember(Order = 6)]
        public DateTime RunningSince { get; set; }

        [YamlMember(Order = 7)]
        public int TotalJobs { get; set; }

        [YamlMember(Order = 8)]
        public int TotalGroups { get; set; }

        [YamlMember(Order = 9)]
        public string JobStoreType { get; set; }

        [YamlMember(Order = 10)]
        public string QuartzVersion { get; set; }
    }
}