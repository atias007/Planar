using System;

namespace Planar.Client.Entities
{
    public partial class ClusterNode
    {
#if NETSTANDARD2_0
        public string Server { get; set; }
        public string InstanceId { get; set; }

#else
        public string Server { get; set; } = null!;
        public string InstanceId { get; set; } = null!;

#endif

        public short Port { get; set; }

        public short ClusterPort { get; set; }

        public DateTime JoinDate { get; set; }

        public DateTime HealthCheckDate { get; set; }

        public int MaxConcurrency { get; set; }

        public TimeSpan HealthCheckGap => DateTime.UtcNow.Subtract(HealthCheckDate.ToUniversalTime());

        public TimeSpan HealthCheckGapDeviation { get; set; }

        public bool LiveNode { get; set; }
    }
}