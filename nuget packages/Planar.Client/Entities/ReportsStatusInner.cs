using System;

namespace Planar.Client.Entities
{
    public class ReportsStatus
    {
        public ReportPeriods Period { get; set; }
        public bool Enabled { get; set; }
        public string? Group { get; set; }
        public DateTime? NextRunning { get; set; }
    }
}