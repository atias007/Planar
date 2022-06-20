using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetryDemoJob
{
    public class Worker : BaseJob
    {
        public override void Configure(IConfigurationBuilder configurationBuilder)
        {
        }

        public override Task ExecuteJob(IJobExecutionContext context)
        {
            Logger.LogInformation("Lets throw some exception and check for retry...");
            throw new ApplicationException("This is some test exception");
        }

        public override void RegisterServices(IServiceCollection services)
        {
        }
    }
}