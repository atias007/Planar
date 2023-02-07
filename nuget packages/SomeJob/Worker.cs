using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar;

namespace SomeJob
{
    public class Worker : BaseJob
    {
        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        public override Task ExecuteJob(IJobExecutionContext context)
        {
            var maxDiffranceHours = Configuration.GetValue("Max Diffrance Hours", 12);
            Logger.LogInformation("Value is {Value}", maxDiffranceHours);
            return Task.CompletedTask;
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
        }
    }
}