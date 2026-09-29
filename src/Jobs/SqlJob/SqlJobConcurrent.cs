using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;
using Quartz;

namespace Planar;

[PersistJobDataAfterExecution]
public class SqlJobConcurrent(
    ILogger<SqlJobConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    Lazy<IJobResourceDataLayer> resourceDal,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil) : SqlJob(logger, dataLayer, resourceDal, jobMonitorUtil, clusterUtil)
{
}