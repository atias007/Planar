using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Service.General;
using Quartz;

namespace Planar;

[PersistJobDataAfterExecution]
public class ProcessJobConcurrent(
    ILogger<ProcessJobConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil,
    IClusterUtil clusterUtil
        ) : ProcessJob(logger, dataLayer, jobMonitorUtil, clusterUtil)
{
}