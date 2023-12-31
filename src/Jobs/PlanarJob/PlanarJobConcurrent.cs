using Microsoft.Extensions.Logging;
using Planar.Common;
using PlanarJob.Notify;

namespace Planar
{
    public class PlanarJobConcurrent : PlanarJob
    {
        public PlanarJobConcurrent(
            ILogger<PlanarJob> logger,
            IJobPropertyDataLayer dataLayer,
            IMonitorUtil monitorUtil,
            NotifyProducer notifyProducer) : base(logger, dataLayer, monitorUtil, notifyProducer)
        {
        }
    }
}