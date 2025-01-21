using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;
using Quartz;

namespace Planar;

[DisallowConcurrentExecution]
[PersistJobDataAfterExecution]
public class SqlTableReportJobNoConcurrent(
    ILogger<SqlTableReportJob> logger,
    IJobPropertyDataLayer dataLayer,
    IGroupDataLayer groupData,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil) : SqlTableReportJob(logger, dataLayer, groupData, jobMonitorUtil, clusterUtil)
{
}