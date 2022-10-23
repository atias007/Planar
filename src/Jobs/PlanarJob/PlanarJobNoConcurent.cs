using Microsoft.Extensions.Logging;
using Quartz;

namespace Planar
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class PlanarJobNoConcurent : PlanarJob
    {
        public PlanarJobNoConcurent(ILogger<PlanarJob> logger) : base(logger)
        {
        }
    }
}