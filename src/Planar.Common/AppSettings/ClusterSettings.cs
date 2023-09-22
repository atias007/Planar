using System;

namespace Planar.Common
{
    public class ClusterSettings
    {
        public TimeSpan CheckinInterval { get; set; }

        public TimeSpan CheckinMisfireThreshold { get; set; }

        public TimeSpan HealthCheckInterval { get; set; }

        public short Port { get; set; }

        public bool Clustering { get; set; }
    }
}