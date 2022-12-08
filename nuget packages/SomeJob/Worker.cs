using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planar;

namespace SomeJob
{
    public class Worker : BaseJob
    {
        public override void Configure(IConfigurationBuilder configurationBuilder, string environment)
        {
        }

        public override Task ExecuteJob(IJobExecutionContext context)
        {
            var maxDiffranceHours = Configuration.GetValue<int>("Max Diffrance Hours", 12);
            return Task.CompletedTask;
        }

        public override void RegisterServices(IServiceCollection services)
        {
        }
    }
}