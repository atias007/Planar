using System;

namespace Planar.Client.Entities
{
    public class ReportsStatus
    {
        public ReportPeriods Period { get; set; }
        public bool Enabled { get; set; }
#if NETSTANDARD2_0
        public string Group { get; set; }
#else
        public string? Group { get; set; }
#endif
        public DateTime? NextRunning { get; set; }
    }
}