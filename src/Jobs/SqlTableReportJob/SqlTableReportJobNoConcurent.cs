using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar;

[DisallowConcurrentExecution]
[PersistJobDataAfterExecution]
public class SqlTableReportJobNoConcurrent(
    ILogger<SqlTableReportJob> logger,
    IJobPropertyDataLayer dataLayer,
    IGroupDataLayer groupData,
    JobMonitorUtil jobMonitorUtil) : SqlTableReportJob(logger, dataLayer, groupData, jobMonitorUtil)
{
}