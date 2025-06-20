using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;
using System;

namespace Planar;

public class PlanarJobConcurrent(
    ILogger<PlanarJobConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil,
    IServiceProvider serviceProvider) : PlanarJob(logger, dataLayer, jobMonitorUtil, clusterUtil, serviceProvider)
{
}