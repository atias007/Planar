using System;

namespace Planar.Client.Entities
{
    internal class ReportsStatusInner
    {
        public string Period { get; set; } = null!;
        public bool Enabled { get; set; }
        public string? Group { get; set; }
        public DateTime? NextRunning { get; set; }
    }

    public class ReportsStatus
    {
        public ReportPeriods Period { get; set; }
        public bool Enabled { get; set; }
        public string? Group { get; set; }
        public DateTime? NextRunning { get; set; }
    }
}