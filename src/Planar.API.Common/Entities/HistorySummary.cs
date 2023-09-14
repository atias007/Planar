﻿namespace Planar.API.Common.Entities
{
    public class HistorySummary
    {
        public string JobId { get; set; } = null!;
        public string JobName { get; set; } = null!;
        public string JobGroup { get; set; } = null!;
        public string JobType { get; set; } = null!;
        public int TotalRuns { get; set; }
        public int Success { get; set; }
        public int Fail { get; set; }
        public int Running { get; set; }
        public int Retries { get; set; }
    }
}