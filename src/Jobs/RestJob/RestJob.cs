using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Quartz;

namespace RestJob
{
    public class RestJob : BaseCommonJob<RestJob, RestJobProperties>
    {
        public RestJob(ILogger<RestJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }

        public override Task Execute(IJobExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}