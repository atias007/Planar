using System;

namespace Planar.API.Common.Entities
{
    public class JobStatistic
    {
        public TimeSpan AvgDuration { get; set; }
        public TimeSpan StdevDuration { get; set; }
        public decimal AvgEffectedRows { get; set; }
        public decimal StdevEffectedRows { get; set; }
    }
}