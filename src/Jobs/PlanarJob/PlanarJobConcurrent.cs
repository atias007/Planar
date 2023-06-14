using Microsoft.Extensions.Logging;
using Planar.Common;

namespace Planar
{
    public class PlanarJobConcurrent : PlanarJob
    {
        public PlanarJobConcurrent(ILogger<PlanarJob> logger, IJobPropertyDataLayer dataLayer, IMonitorUtil monitorUtil) : base(logger, dataLayer, monitorUtil)
        {
        }
    }
}