using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace CancelTokenTest
{
    internal class Job : BaseJob
    {
        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            Logger.LogInformation($"Strat delay of 60sec... to cancel run: job cancel {context.JobDetails.Key.Group}.{context.JobDetails.Key.Name}...");
            await Task.Delay(60000);
            Logger.LogInformation("End delay");
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
        }
    }
}