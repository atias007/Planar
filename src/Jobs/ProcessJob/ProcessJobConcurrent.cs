using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar
{
    [PersistJobDataAfterExecution]
    public class ProcessJobConcurrent(
        ILogger<ProcessJob> logger,
        IJobPropertyDataLayer dataLayer,
        JobMonitorUtil jobMonitorUtil
            ) : ProcessJob(logger, dataLayer, jobMonitorUtil)
    {
    }
}