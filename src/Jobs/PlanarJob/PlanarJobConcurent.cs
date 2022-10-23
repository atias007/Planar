using Microsoft.Extensions.Logging;

namespace Planar
{
    public class PlanarJobConcurent : PlanarJob
    {
        public PlanarJobConcurent(ILogger<PlanarJob> logger) : base(logger)
        {
        }
    }
}