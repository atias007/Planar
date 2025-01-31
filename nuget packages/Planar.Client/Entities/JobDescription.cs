namespace Planar.Client.Entities
{
    public class JobDescription
    {
#if NETSTANDARD2_0
        public JobDetails Details { get; set; }
        public JobMetrics Metrics { get; set; }
        public PagingResponse<MonitorDetails> Monitors { get; set; }
        public PagingResponse<JobAudit> Audits { get; set; }
        public PagingResponse<JobInstanceBasicLog> History { get; set; }
#else
        public JobDetails Details { get; set; } = null!;
        public JobMetrics Metrics { get; set; } = null!;
        public PagingResponse<MonitorDetails> Monitors { get; set; } = null!;
        public PagingResponse<JobAudit> Audits { get; set; } = null!;
        public PagingResponse<JobInstanceBasicLog> History { get; set; } = null!;
#endif
    }
}