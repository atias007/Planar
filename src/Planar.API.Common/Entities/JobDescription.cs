using System.Collections.Generic;

namespace Planar.API.Common.Entities
{
    public class JobDescription
    {
        public JobDetails Details { get; set; } = null!;
        public JobStatistic Statistics { get; set; } = null!;
        public PagingResponse<MonitorItem> Monitors { get; set; } = null!;
        public PagingResponse<JobAuditDto> Audits { get; set; } = null!;
        public PagingResponse<JobInstanceLogRow> History { get; set; } = null!;
    }
}