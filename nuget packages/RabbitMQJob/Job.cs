using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planar.Job;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQJob
{
    internal class Job : BaseJob
    {
        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
            throw new NotImplementedException();
        }

        public override Task ExecuteJob(IJobExecutionContext context)
        {
            throw new NotImplementedException();
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}