using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class PlanarJobNoConcurent : PlanarJob
    {
        public PlanarJobNoConcurent(ILogger<PlanarJob> logger, IJobPropertyDataLayer dataLayer, IMonitorUtil monitorUtil) : base(logger, dataLayer, monitorUtil)
        {
        }
    }
}