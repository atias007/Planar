using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar
{
    public class PlanarJobConcurent : PlanarJob
    {
        public PlanarJobConcurent(ILogger<PlanarJob> logger, IJobPropertyDataLayer dataLayer, IMonitorUtil monitorUtil) : base(logger, dataLayer, monitorUtil)
        {
        }
    }
}