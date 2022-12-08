using Microsoft.Extensions.Logging;
using Planar.Common;

namespace Planar
{
    public class PlanarJobConcurent : PlanarJob
    {
        public PlanarJobConcurent(ILogger<PlanarJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }
    }
}