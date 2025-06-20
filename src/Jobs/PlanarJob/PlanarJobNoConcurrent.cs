using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;
using Quartz;
using System;

namespace Planar;

[DisallowConcurrentExecution]
[PersistJobDataAfterExecution]
public class PlanarJobNoConcurrent(
    ILogger<PlanarJobNoConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil,
    IServiceProvider serviceProvider) : PlanarJob(logger, dataLayer, jobMonitorUtil, clusterUtil, serviceProvider)
{
}