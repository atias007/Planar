using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar;

[PersistJobDataAfterExecution]
public class RestJobConcurrent(
    ILogger<RestJobConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil
        ) : RestJob(logger, dataLayer, jobMonitorUtil)
{
}