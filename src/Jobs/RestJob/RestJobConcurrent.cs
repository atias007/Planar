using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;
using Quartz;

namespace Planar;

[PersistJobDataAfterExecution]
public class RestJobConcurrent(
    ILogger<RestJobConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    Lazy<IJobResourceDataLayer> resourceDal,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil) : RestJob(logger, dataLayer, resourceDal, jobMonitorUtil, clusterUtil)
{
}