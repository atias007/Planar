using System;

namespace Planar.Service.Model
{
    public partial class ClusterNode
    {
        public TimeSpan? HealthCheckGap
        {
            get
            {
                return HealthCheckDate.HasValue ? DateTime.Now.Subtract(HealthCheckDate.Value) : null;
            }
        }

        public TimeSpan? HealthCheckGapDeviation
        {
            get
            {
                return HealthCheckGap.HasValue ? HealthCheckGap.Value.Subtract(AppSettings.ClusterHealthCheckInterval) : null;
            }
        }

        public static bool operator ==(ClusterNode a, ClusterNode b)
        {
            if (a is null && b is null) { return true; }
            if (a is null || b is null) { return false; }

            return string.Equals(a.Server, b.Server, StringComparison.CurrentCultureIgnoreCase) && a.Port == b.Port;
        }

        public static bool operator !=(ClusterNode a, ClusterNode b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return this == obj as ClusterNode;
        }

        public override int GetHashCode()
        {
            return $"{Server}|{Port}".GetHashCode();
        }
    }
}