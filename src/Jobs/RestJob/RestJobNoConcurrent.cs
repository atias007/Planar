using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class RestJobNoConcurrent(
        ILogger<RestJob> logger,
        IJobPropertyDataLayer dataLayer,
        JobMonitorUtil jobMonitorUtil) : RestJob(logger, dataLayer, jobMonitorUtil)
    {
    }
}