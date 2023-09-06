using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using System.Threading.Tasks;

namespace RetryDemoJob
{
    public class Worker : BaseJob
    {
        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        public override Task ExecuteJob(IJobExecutionContext context)
        {
            var isRetryTrigger = context.TriggerDetails.IsRetryTrigger;
            var hasRetry = context.TriggerDetails.HasRetry;
            var retryNumber = context.TriggerDetails.RetryNumber;
            var isLastRetry = context.TriggerDetails.IsLastRetry;
            var maxRetries = context.TriggerDetails.MaxRetries;

            Logger.LogInformation("HasRetry={HasRetry}, IsRetryTrigger={IsRetryTrigger}, RetryNumber={RetryNumber}, MaxRetries={MaxRetries}, IsLastRetry={IsLastRetry}",
                hasRetry, isRetryTrigger, retryNumber, maxRetries, isLastRetry);

            Logger.LogInformation("Lets throw some exception and check for retry...");
            try
            {
                throw new PlanarJobException("This is some test exception");
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "Exception thrown, lets see if we retry...");
                throw;
            }
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
        }
    }
}