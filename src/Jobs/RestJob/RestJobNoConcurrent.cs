using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace RestJob
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class RestJobNoConcurrent : RestJob
    {
        public RestJobNoConcurrent(ILogger<RestJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }
    }
}