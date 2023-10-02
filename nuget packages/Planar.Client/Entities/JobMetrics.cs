using System;

namespace Planar.Client.Entities
{
    public class JobMetrics
    {
        public TimeSpan AvgDuration { get; set; }
        public TimeSpan StdevDuration { get; set; }
        public decimal AvgEffectedRows { get; set; }
        public decimal StdevEffectedRows { get; set; }
        public int TotalRuns { get; set; }
        public int SuccessRetries { get; set; }
        public int FailRetries { get; set; }
        public int Recovers { get; set; }
    }
}