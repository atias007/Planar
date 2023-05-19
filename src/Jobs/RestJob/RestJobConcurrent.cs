using Microsoft.Extensions.Logging;
using Planar.Common;

namespace RestJob
{
    public class RestJobConcurrent : RestJob
    {
        public RestJobConcurrent(ILogger<RestJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }
    }
}