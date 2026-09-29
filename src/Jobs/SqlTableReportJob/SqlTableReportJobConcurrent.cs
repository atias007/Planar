using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;
using Quartz;

namespace Planar;

[PersistJobDataAfterExecution]
public class SqlTableReportJobConcurrent(
    ILogger<SqlTableReportJobConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    IGroupDataLayer groupData,
    Lazy<IJobResourceDataLayer> resourceDal,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil) : SqlTableReportJob(logger, dataLayer, groupData, resourceDal, jobMonitorUtil, clusterUtil)
{
}