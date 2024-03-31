using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar;

[PersistJobDataAfterExecution]
public class SqlJobConcurrent(
    ILogger<SqlJobConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil) : SqlJob(logger, dataLayer, jobMonitorUtil)
{
}