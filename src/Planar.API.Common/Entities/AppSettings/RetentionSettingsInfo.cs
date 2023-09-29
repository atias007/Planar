namespace Planar.API.Common.Entities
{
    public class RetentionSettingsInfo
    {
        public int TraceRetentionDays { get; set; }

        public int JobLogRetentionDays { get; set; }

        public int StatisticsRetentionDays { get; set; }
    }
}