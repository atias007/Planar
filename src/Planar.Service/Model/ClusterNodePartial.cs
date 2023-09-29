using Planar.Common;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planar.Service.Model
{
    public partial class ClusterNode
    {
        [NotMapped]
        public TimeSpan HealthCheckGap => DateTime.UtcNow.Subtract(HealthCheckDate.ToUniversalTime());

        [NotMapped]
        public TimeSpan HealthCheckGapDeviation
        {
            get
            {
                return HealthCheckGap.Subtract(AppSettings.Cluster.CheckinInterval);
            }
        }

        [NotMapped]
        public bool LiveNode
        {
            get
            {
                return HealthCheckGapDeviation.TotalSeconds < 30;
            }
        }

        [NotMapped]
        internal bool IsCurrentNode { get; set; }
    }
}