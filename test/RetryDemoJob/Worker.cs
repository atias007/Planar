using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar;
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
            Logger.LogInformation("Lets throw some exception and check for retry...");
            throw new PlanarJobException("This is some test exception");
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
        }
    }
}