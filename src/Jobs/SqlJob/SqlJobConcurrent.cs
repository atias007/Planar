using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar
{
    [PersistJobDataAfterExecution]
    public class SqlJobConcurrent : SqlJob
    {
        public SqlJobConcurrent(
            ILogger<SqlJob> logger,
            IJobPropertyDataLayer dataLayer,
            JobMonitorUtil jobMonitorUtil) : base(logger, dataLayer, jobMonitorUtil)
        {
        }
    }
}