using System;

namespace Planar.API.Common.Entities
{
    public class ClusterSettingsInfo
    {
        public TimeSpan CheckinInterval { get; set; }

        public TimeSpan CheckinMisfireThreshold { get; set; }

        public TimeSpan HealthCheckInterval { get; set; }

        public short Port { get; set; }

        public bool Clustering { get; set; }
    }
}