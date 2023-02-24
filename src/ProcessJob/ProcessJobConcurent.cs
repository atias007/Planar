using Microsoft.Extensions.Logging;
using Planar.Common;

namespace Planar
{
    public class ProcessJobConcurent : ProcessJob
    {
        public ProcessJobConcurent(ILogger<ProcessJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }
    }
}