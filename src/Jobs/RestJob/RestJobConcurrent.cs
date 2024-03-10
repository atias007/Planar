using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar
{
    [PersistJobDataAfterExecution]
    public class RestJobConcurrent(
        ILogger<RestJob> logger,
        IJobPropertyDataLayer dataLayer,
        JobMonitorUtil jobMonitorUtil
            ) : RestJob(logger, dataLayer, jobMonitorUtil)
    {
    }
}