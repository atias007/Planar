using Microsoft.Extensions.Logging;
using Planar.Common;

namespace Planar
{
    public class RestJobConcurrent : RestJob
    {
        public RestJobConcurrent(ILogger<RestJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }
    }
}