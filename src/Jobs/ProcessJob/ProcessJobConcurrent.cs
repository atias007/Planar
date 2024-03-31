using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar;

[PersistJobDataAfterExecution]
public class ProcessJobConcurrent(
    ILogger<ProcessJobConcurrent> logger,
    IJobPropertyDataLayer dataLayer,
    JobMonitorUtil jobMonitorUtil
        ) : ProcessJob(logger, dataLayer, jobMonitorUtil)
{
}