using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class SqlJobNoConcurrent(
        ILogger<SqlJob> logger,
        IJobPropertyDataLayer dataLayer,
        JobMonitorUtil jobMonitorUtil) : SqlJob(logger, dataLayer, jobMonitorUtil)
    {
    }
}