using System;

namespace Planar.Service.Model
{
    public partial class ClusterNode
    {
        public TimeSpan HealthCheckGap
        {
            get
            {
                return DateTime.Now.Subtract(HealthCheckDate);
            }
        }

        public TimeSpan HealthCheckGapDeviation
        {
            get
            {
                return HealthCheckGap.Subtract(AppSettings.ClusterHealthCheckInterval);
            }
        }

        public bool LiveNode
        {
            get
            {
                return HealthCheckGapDeviation.TotalSeconds < 30;
            }
        }

        public bool IsCurrentNode { get; set; }
    }
}