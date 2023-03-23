using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace HelloWorldJob
{
    public class Job : BaseJob
    {
        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
        }

        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            for (int i = 1; i <= 10; i++)
            {
                Logger.LogInformation("Hello world - Round {Index}", i);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }
}