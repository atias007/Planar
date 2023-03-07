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
        public bool Clustering { get; set; }

        [YamlMember(Order = 7)]
        public int MaxConcurrency { get; set; }

        [YamlMember(Order = 8)]
        public TimeSpan ClusteringCheckinInterval { get; set; }

        [YamlMember(Order = 9)]
        public TimeSpan ClusteringCheckinMisfireThreshold { get; set; }

        [YamlMember(Order = 10)]
        public int ClearTraceTableOverDays { get; set; }

        [YamlMember(Order = 11)]
        public int ClearJobLogTableOverDays { get; set; }

        [YamlMember(Order = 12)]
        public int ClearStatisticsTablesOverDays { get; set; }

        [YamlMember(Order = 13)]
        public short HttpPort { get; set; }

        [YamlMember(Order = 14)]
        public short HttpsPort { get; set; }

        [YamlMember(Order = 15)]
        public bool UseHttpsRedirect { get; set; }

        [YamlMember(Order = 16)]
        public bool UseHttps { get; set; }

        [YamlMember(Order = 17)]
        public short ClusterPort { get; set; }

        [YamlMember(Order = 18)]
        public string LogLevel { get; set; }

        [YamlMember(Order = 19)]
        public bool SwaggerUI { get; set; }

        [YamlMember(Order = 20)]
        public bool OpenApiUI { get; set; }

        [YamlMember(Order = 21)]
        public bool DeveloperExceptionPage { get; set; }

        [YamlMember(Order = 22)]
        public string? AuthenticationMode { get; set; }

        [YamlMember(Order = 96)]
        public DateTime? RunningSince { get; set; }

        [YamlMember(Order = 97)]
        public int TotalJobs { get; set; }

        [YamlMember(Order = 98)]
        public int TotalGroups { get; set; }

        [YamlMember(Order = 99)]
        public string DatabaseProvider { get; set; }

        [YamlMember(Order = 100)]
        public string QuartzVersion { get; set; }

        [YamlMember(Order = 101)]
        public string ServiceVersion { get; set; }

        [YamlMember(Order = 102)]
        public string CliVersion { get; set; }
    }
}