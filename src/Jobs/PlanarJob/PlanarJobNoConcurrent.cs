using Microsoft.Extensions.Logging;
using Planar.Common;
using PlanarJob.Notify;
using Quartz;

namespace Planar
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class PlanarJobNoConcurrent : PlanarJob
    {
        public PlanarJobNoConcurrent(
            ILogger<PlanarJob> logger,
            IJobPropertyDataLayer dataLayer,
            IMonitorUtil monitorUtil,
            NotifyProducer notifyProducer) : base(logger, dataLayer, monitorUtil, notifyProducer)
        {
        }
    }
}