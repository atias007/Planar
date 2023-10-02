namespace Planar.Client.Entities
{
    public class JobDescription
    {
        public JobDetails Details { get; set; } = null!;
        public JobMetrics Metrics { get; set; } = null!;
        public PagingResponse<MonitorItem> Monitors { get; set; } = null!;
        public PagingResponse<JobAudit> Audits { get; set; } = null!;
        public PagingResponse<JobInstanceBasicLog> History { get; set; } = null!;
    }
}