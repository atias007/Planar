using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace Planar
{
    public class PlanarJobConcurent : PlanarJob
    {
        public PlanarJobConcurent(ILogger<PlanarJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }
    }
}