using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;

namespace Planar
{
    public class RestJobConcurrent : RestJob
    {
        public RestJobConcurrent(
            ILogger<RestJob> logger,
            IJobPropertyDataLayer dataLayer,
            JobMonitorUtil jobMonitorUtil
            ) : base(logger, dataLayer, jobMonitorUtil)
        {
        }
    }
}