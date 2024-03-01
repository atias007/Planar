using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;

namespace Planar
{
    public class PlanarJobConcurrent : PlanarJob
    {
        public PlanarJobConcurrent(
            ILogger<PlanarJob> logger,
            IJobPropertyDataLayer dataLayer,
            JobMonitorUtil jobMonitorUtil) : base(logger, dataLayer, jobMonitorUtil)
        {
        }
    }
}