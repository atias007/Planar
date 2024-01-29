using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class RestJobNoConcurrent : RestJob
    {
        public RestJobNoConcurrent(
            ILogger<RestJob> logger,
            IJobPropertyDataLayer dataLayer,
            JobMonitorUtil jobMonitorUtil) : base(logger, dataLayer, jobMonitorUtil)
        {
        }
    }
}