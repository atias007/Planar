using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;
using Quartz;

namespace Planar;

[DisallowConcurrentExecution]
[PersistJobDataAfterExecution]
public class RestJobNoConcurrent(
    ILogger<RestJobNoConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    Lazy<IJobResourceDataLayer> resourceDal,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil) : RestJob(logger, dataLayer, resourceDal, jobMonitorUtil, clusterUtil)
{
}