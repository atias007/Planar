using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar;

[PersistJobDataAfterExecution]
public class SqlTableReportJobConcurrent(
    ILogger<SqlTableReportJobConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    IGroupDataLayer groupData,
    JobMonitorUtil jobMonitorUtil) : SqlTableReportJob(logger, dataLayer, groupData, jobMonitorUtil)
{
}