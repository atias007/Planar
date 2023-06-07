using System.Collections.Generic;

namespace Planar.API.Common.Entities
{
    public class JobDescription
    {
        public JobDetails Details { get; set; } = null!;
        public IEnumerable<MonitorItem> Monitors { get; set; } = null!;
        public IEnumerable<JobAuditDto> Audits { get; set; } = null!;
        public IEnumerable<JobInstanceLogRow> History { get; set; } = null!;
    }
}