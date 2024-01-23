using System;

namespace Planar.Client.Entities
{
    public partial class ClusterNode
    {
        public string Server { get; set; } = null!;

        public short Port { get; set; }

        public string InstanceId { get; set; } = null!;

        public short ClusterPort { get; set; }

        public DateTime JoinDate { get; set; }

        public DateTime HealthCheckDate { get; set; }

        public int MaxConcurrency { get; set; }

        public TimeSpan HealthCheckGap => DateTime.UtcNow.Subtract(HealthCheckDate.ToUniversalTime());

        public TimeSpan HealthCheckGapDeviation { get; set; }

        public bool LiveNode { get; set; }
    }
}