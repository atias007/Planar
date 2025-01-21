using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;

namespace Planar;

public class PlanarJobConcurrent(
    ILogger<PlanarJobConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil) : PlanarJob(logger, dataLayer, jobMonitorUtil, clusterUtil)
{
}