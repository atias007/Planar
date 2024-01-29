using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class ProcessJobNoConcurrent : ProcessJob
    {
        public ProcessJobNoConcurrent(
            ILogger<ProcessJob> logger,
            IJobPropertyDataLayer dataLayer,
            JobMonitorUtil jobMonitorUtil) : base(logger, dataLayer, jobMonitorUtil)
        {
        }
    }
}